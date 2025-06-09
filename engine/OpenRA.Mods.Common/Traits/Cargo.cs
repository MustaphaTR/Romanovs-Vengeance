#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor can transport Passenger actors.")]
	public class CargoInfo : PausableConditionalTraitInfo, Requires<IOccupySpaceInfo>
	{
		[Desc("The maximum sum of Passenger.Weight that this actor can support.")]
		public readonly int MaxWeight = 0;

		[Desc("`Passenger.CargoType`s that can be loaded into this actor.")]
		public readonly HashSet<string> Types = [];

		[Desc("A list of actor types that are initially spawned into this actor.")]
		public readonly string[] InitialUnits = [];

		[Desc("When cargo is full, unload the first passenger instead of disabling loading.")]
		public readonly bool ReplaceFirstWhenFull = false;

		[Desc("When this actor is sold should all of its passengers be unloaded?")]
		public readonly bool EjectOnSell = true;

		[Desc("When this actor dies this much percent of passengers total health is dealt to them.")]
		public readonly int EjectOnDeathDamage = 100;

		[Desc("Terrain types that this actor is allowed to eject actors onto. Leave empty for all terrain types.")]
		public readonly HashSet<string> UnloadTerrainTypes = [];

		[VoiceReference]
		[Desc("Voice to play when ordered to unload the passengers.")]
		public readonly string UnloadVoice = "Action";

		[Desc("Radius to search for a load/unload location if the ordered cell is blocked.")]
		public readonly WDist LoadRange = WDist.FromCells(5);

		[Desc("Which direction the passenger will face (relative to the transport) when unloading.")]
		public readonly WAngle PassengerFacing = new(512);

		[Desc("Delay (in ticks) before continuing after loading a passenger.")]
		public readonly int AfterLoadDelay = 8;

		[Desc("Delay (in ticks) before unloading the first passenger.")]
		public readonly int BeforeUnloadDelay = 8;

		[Desc("Delay (in ticks) before continuing after unloading a passenger.")]
		public readonly int AfterUnloadDelay = 25;

		[CursorReference]
		[Desc("Cursor to display when able to unload the passengers.")]
		public readonly string UnloadCursor = "deploy";

		[CursorReference]
		[Desc("Cursor to display when unable to unload the passengers.")]
		public readonly string UnloadBlockedCursor = "deploy-blocked";

		[GrantedConditionReference]
		[Desc("The condition to grant to self while waiting for cargo to load.")]
		public readonly string LoadingCondition = null;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while passengers are loaded.",
			"Condition can stack with multiple passengers.")]
		public readonly string LoadedCondition = null;

		[ActorReference(dictionaryReference: LintDictionaryReference.Keys)]
		[Desc("Conditions to grant when specified actors are loaded inside the transport.",
			"A dictionary of [actor name]: [condition].")]
		public readonly Dictionary<string, string> PassengerConditions = [];

		[Desc("Change the passengers owner if transport owner changed")]
		public readonly bool OwnerChangedAffectsPassengers = true;

		[GrantedConditionReference]
		public IEnumerable<string> LinterPassengerConditions => PassengerConditions.Values;

		public override object Create(ActorInitializer init) { return new Cargo(init, this); }
	}

	public class Cargo : PausableConditionalTrait<CargoInfo>, IIssueOrder, IResolveOrder, IOrderVoice,
		INotifyOwnerChanged, INotifySold, INotifyActorDisposing, IIssueDeployOrder,
		INotifyCreated, INotifyKilled, ITransformActorInitModifier, INotifyPassengersDamage
	{
		readonly Actor self;
		readonly List<Actor> cargo = [];
		readonly HashSet<Actor> reserves = [];
		readonly Dictionary<string, Stack<int>> passengerTokens = [];
		readonly Lazy<IFacing> facing;
		readonly bool checkTerrainType;

		public int TotalWeight = 0;
		int reservedWeight = 0;
		Aircraft aircraft;
		int loadingToken = Actor.InvalidConditionToken;
		readonly Stack<int> loadedTokens = [];
		bool takeOffAfterLoad;
		bool initialised;

		public IEnumerable<Actor> Passengers => cargo;
		public int PassengerCount => cargo.Count;

		enum State { Free, Locked }
		State state = State.Free;

		public Cargo(ActorInitializer init, CargoInfo info)
			: base(info)
		{
			self = init.Self;
			checkTerrainType = info.UnloadTerrainTypes.Count > 0;

			var runtimeCargoInit = init.GetOrDefault<RuntimeCargoInit>(info);
			var cargoInit = init.GetOrDefault<CargoInit>(info);
			if (runtimeCargoInit != null)
			{
				cargo = runtimeCargoInit.Value.ToList();
				TotalWeight = cargo.Sum(GetWeight);
			}
			else if (cargoInit != null)
			{
				foreach (var u in cargoInit.Value)
				{
					var unit = self.World.CreateActor(false, u.ToLowerInvariant(),
						[new OwnerInit(self.Owner)]);

					cargo.Add(unit);
				}

				TotalWeight = cargo.Sum(GetWeight);
			}
			else
			{
				foreach (var u in info.InitialUnits)
				{
					var unit = self.World.CreateActor(false, u.ToLowerInvariant(),
						[new OwnerInit(self.Owner)]);

					cargo.Add(unit);
				}

				TotalWeight = cargo.Sum(GetWeight);
			}

			facing = Exts.Lazy(self.TraitOrDefault<IFacing>);
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			aircraft = self.TraitOrDefault<Aircraft>();

			if (cargo.Count > 0)
			{
				foreach (var c in cargo)
					if (Info.PassengerConditions.TryGetValue(c.Info.Name, out var passengerCondition))
						passengerTokens.GetOrAdd(c.Info.Name).Push(self.GrantCondition(passengerCondition));

				if (!string.IsNullOrEmpty(Info.LoadedCondition))
					loadedTokens.Push(self.GrantCondition(Info.LoadedCondition));
			}

			// Defer notifications until we are certain all traits on the transport are initialised
			self.World.AddFrameEndTask(w =>
			{
				foreach (var c in cargo)
				{
					c.Trait<Passenger>().Transport = self;

					foreach (var nec in c.TraitsImplementing<INotifyEnteredCargo>())
						nec.OnEnteredCargo(c, self);

					foreach (var npe in self.TraitsImplementing<INotifyPassengerEntered>())
						npe.OnPassengerEntered(self, c);
				}

				initialised = true;
			});
		}

		static int GetWeight(Actor a) { return a.Info.TraitInfo<PassengerInfo>().Weight; }

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				if (IsTraitDisabled)
					yield break;

				yield return new DeployOrderTargeter("Unload", 10,
					() => CanUnload() ? Info.UnloadCursor : Info.UnloadBlockedCursor);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "Unload")
				return new Order(order.OrderID, self, queued);

			return null;
		}

		Order IIssueDeployOrder.IssueDeployOrder(Actor self, bool queued)
		{
			return new Order("Unload", self, queued);
		}

		bool IIssueDeployOrder.CanIssueDeployOrder(Actor self, bool queued) { return true; }

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Unload")
			{
				if (!order.Queued && !CanUnload())
					return;

				self.QueueActivity(order.Queued, new UnloadCargo(self, Info.LoadRange));
			}
		}

		public IEnumerable<CPos> CurrentAdjacentCells()
		{
			return Util.AdjacentCells(self.World, Target.FromActor(self)).Where(c => self.Location != c);
		}

		public bool CanUnload(BlockedByActor check = BlockedByActor.None)
		{
			if (IsTraitDisabled)
				return false;

			if (checkTerrainType)
			{
				if (!self.World.Map.Contains(self.Location))
					return false;

				if (!Info.UnloadTerrainTypes.Contains(self.World.Map.GetTerrainInfo(self.Location).Type))
					return false;
			}

			return !IsEmpty() && (aircraft == null || aircraft.CanLand(self.Location, blockedByMobile: false)) && !IsTraitPaused
				&& CurrentAdjacentCells().Any(c => Passengers.Any(p => !p.IsDead && p.Trait<IPositionable>().CanEnterCell(c, null, check)));
		}

		public bool CanLoad(Actor a)
		{
			return !IsTraitDisabled && !IsTraitPaused && (reserves.Contains(a) || HasSpace(GetWeight(a)));
		}

		internal bool ReserveSpace(Actor a)
		{
			if (reserves.Contains(a))
				return true;

			var w = GetWeight(a);
			if (!HasSpace(w))
				return false;

			if (loadingToken == Actor.InvalidConditionToken)
				loadingToken = self.GrantCondition(Info.LoadingCondition);

			reserves.Add(a);
			reservedWeight += w;
			LockForPickup(self);

			return true;
		}

		internal void UnreserveSpace(Actor a)
		{
			if (!reserves.Contains(a) || self.IsDead)
				return;

			reservedWeight -= GetWeight(a);
			reserves.Remove(a);
			ReleaseLock(self);

			if (loadingToken != Actor.InvalidConditionToken)
				loadingToken = self.RevokeCondition(loadingToken);
		}

		// Prepare for transport pickup
		void LockForPickup(Actor self)
		{
			if (state == State.Locked)
				return;

			state = State.Locked;

			self.CancelActivity();

			var air = self.TraitOrDefault<Aircraft>();
			if (air != null && !air.AtLandAltitude)
			{
				takeOffAfterLoad = true;
				self.QueueActivity(new Land(self));
			}

			self.QueueActivity(new WaitFor(() => state != State.Locked, false));
		}

		void ReleaseLock(Actor self)
		{
			if (reservedWeight != 0)
				return;

			state = State.Free;

			self.QueueActivity(new Wait(Info.AfterLoadDelay, false));
			if (takeOffAfterLoad)
				self.QueueActivity(new TakeOff(self));

			takeOffAfterLoad = false;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString != "Unload" || IsEmpty() || !self.HasVoice(Info.UnloadVoice))
				return null;

			return Info.UnloadVoice;
		}

		public bool HasSpace(int weight) { return Info.ReplaceFirstWhenFull || TotalWeight + reservedWeight + weight <= Info.MaxWeight; }
		public bool IsEmpty() { return cargo.Count == 0; }

		public Actor Peek() { return cargo[^1]; }

		public Actor Unload(Actor self, Actor passenger = null)
		{
			passenger ??= cargo[^1];
			if (!cargo.Remove(passenger))
				throw new ArgumentException("Attempted to unload an actor that is not a passenger.");

			TotalWeight -= GetWeight(passenger);

			SetPassengerFacing(passenger);

			foreach (var npe in self.TraitsImplementing<INotifyPassengerExited>())
				npe.OnPassengerExited(self, passenger);

			foreach (var nec in passenger.TraitsImplementing<INotifyExitedCargo>())
				nec.OnExitedCargo(passenger, self);

			var p = passenger.Trait<Passenger>();
			p.Transport = null;

			if (passengerTokens.TryGetValue(passenger.Info.Name, out var passengerToken) && passengerToken.Count > 0)
				self.RevokeCondition(passengerToken.Pop());

			if (loadedTokens.Count > 0)
				self.RevokeCondition(loadedTokens.Pop());

			return passenger;
		}

		void SetPassengerFacing(Actor passenger)
		{
			if (facing.Value == null)
				return;

			var passengerFacing = passenger.TraitOrDefault<IFacing>();
			if (passengerFacing != null)
				passengerFacing.Facing = facing.Value.Facing + Info.PassengerFacing;
		}

		public (CPos Cell, SubCell SubCell)? ChooseExitSubCell(Actor passenger)
		{
			var pos = passenger.Trait<IPositionable>();

			return CurrentAdjacentCells().Shuffle(self.World.SharedRandom)
				.Select(c => ((CPos Cell, SubCell SubCell)?)(c, pos.GetAvailableSubCell(c)))
				.FirstOrDefault(s => s.Value.SubCell != SubCell.Invalid);
		}

		public void Load(Actor self, Actor a)
		{
			var w = GetWeight(a);
			TotalWeight += w;

			while (Info.ReplaceFirstWhenFull && TotalWeight > Info.MaxWeight)
			{
				var passenger = Unload(self, cargo[0]);
				var cp = self.CenterPosition;
				var inAir = self.World.Map.DistanceAboveTerrain(cp).Length != 0;
				var positionable = passenger.Trait<IPositionable>();
				var exitSubCell = ChooseExitSubCell(passenger);
				if (exitSubCell != null)
					positionable.SetPosition(passenger, exitSubCell.Value.Cell, exitSubCell.Value.SubCell);
				else
					positionable.SetPosition(passenger, self.Location);
				positionable.SetCenterPosition(passenger, self.CenterPosition);

				if (self.Owner.WinState != WinState.Lost && !inAir && positionable.CanEnterCell(self.Location, self, BlockedByActor.None))
				{
					self.World.AddFrameEndTask(world => world.Add(passenger));
					var nbms = passenger.TraitsImplementing<INotifyBlockingMove>();
					foreach (var nbm in nbms)
						nbm.OnNotifyBlockingMove(passenger, passenger);
				}
				else
					passenger.Kill(self);
			}

			cargo.Add(a);
			if (reserves.Contains(a))
			{
				reservedWeight -= w;
				reserves.Remove(a);
				ReleaseLock(self);

				if (loadingToken != Actor.InvalidConditionToken)
					loadingToken = self.RevokeCondition(loadingToken);
			}

			// Don't initialise (effectively twice) if this runs before the FrameEndTask from Created
			if (initialised)
			{
				a.Trait<Passenger>().Transport = self;

				foreach (var nec in a.TraitsImplementing<INotifyEnteredCargo>())
					nec.OnEnteredCargo(a, self);

				foreach (var npe in self.TraitsImplementing<INotifyPassengerEntered>())
					npe.OnPassengerEntered(self, a);
			}

			if (Info.PassengerConditions.TryGetValue(a.Info.Name, out var passengerCondition))
				passengerTokens.GetOrAdd(a.Info.Name).Push(self.GrantCondition(passengerCondition));

			if (!string.IsNullOrEmpty(Info.LoadedCondition))
				loadedTokens.Push(self.GrantCondition(Info.LoadedCondition));
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			// IsAtGroundLevel contains Map.Contains(self.Location) check.
			if (Info.EjectOnDeathDamage < 100 && self.IsAtGroundLevel()
				&& (!checkTerrainType || Info.UnloadTerrainTypes.Contains(self.World.Map.GetTerrainInfo(self.Location).Type)))
			{
				while (!IsEmpty())
				{
					var passenger = Unload(self);
					self.World.AddFrameEndTask(w =>
					{
						var positionable = passenger.Trait<IPositionable>();
						if (positionable.CanEnterCell(self.Location, self, BlockedByActor.All))
						{
							var exitSubCell = ChooseExitSubCell(passenger);
							if (exitSubCell != null)
								positionable.SetPosition(passenger, exitSubCell.Value.Cell, exitSubCell.Value.SubCell);
							else
								positionable.SetPosition(passenger, self.Location);
							positionable.SetCenterPosition(passenger, self.CenterPosition);
							w.Add(passenger);

							var nbms = passenger.TraitsImplementing<INotifyBlockingMove>();
							foreach (var nbm in nbms)
								nbm.OnNotifyBlockingMove(passenger, passenger);

							var health = passenger.TraitOrDefault<Health>();
							if (Info.EjectOnDeathDamage > 0 && health != null)
							{
								var damage = health.MaxHP * Info.EjectOnDeathDamage / 100;
								health.InflictDamage(passenger, e.Attacker, new Damage(damage, e.Damage.DamageTypes), true);
							}

							passenger.Trait<Passenger>().OnEjectedFromKilledCargo(passenger);
						}
						else
							passenger.Kill(e.Attacker);
					});
				}
			}
			else
				foreach (var c in cargo)
					c.Kill(e.Attacker);

			cargo.Clear();
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			foreach (var c in cargo)
				c.Dispose();

			cargo.Clear();
		}

		void INotifySold.Selling(Actor self) { }
		void INotifySold.Sold(Actor self)
		{
			if (!Info.EjectOnSell || cargo == null)
				return;

			while (!IsEmpty())
				SpawnPassenger(Unload(self));
		}

		void SpawnPassenger(Actor passenger)
		{
			self.World.AddFrameEndTask(w =>
			{
				w.Add(passenger);
				passenger.Trait<IPositionable>().SetPosition(passenger, self.Location);

				// TODO: this won't work well for >1 actor as they should move towards the next enterable (sub) cell instead
			});
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (!Info.OwnerChangedAffectsPassengers || cargo == null)
				return;

			foreach (var p in Passengers)
				p.ChangeOwner(newOwner);
		}

		void ITransformActorInitModifier.ModifyTransformActorInit(Actor self, TypeDictionary init)
		{
			init.Add(new RuntimeCargoInit(Info, Passengers.ToArray()));
		}

		static int DamageVersus(Actor victim, Dictionary<string, int> versus)
		{
			// If no Versus values are defined, DamageVersus would return 100 anyway, so we might as well do that early.
			if (versus.Count == 0 || victim.IsDead)
				return 100;

			var armor = victim.TraitsImplementing<Armor>()
				.Where(a => !a.IsTraitDisabled && a.Info.Type != null && versus.ContainsKey(a.Info.Type))
				.Select(a => versus[a.Info.Type]);

			return Util.ApplyPercentageModifiers(100, armor);
		}

		void INotifyPassengersDamage.DamagePassengers(
			int damage, Actor attacker, int amount, Dictionary<string, int> versus, BitSet<DamageType> damageTypes, IEnumerable<int> damageModifiers)
		{
			var passengersToDamage = amount > 0 && amount < cargo.Count ? cargo.Shuffle(self.World.SharedRandom).Take(amount).ToArray() : cargo.ToArray();
			foreach (var passenger in passengersToDamage)
			{
				var d = Util.ApplyPercentageModifiers(damage, damageModifiers.Append(DamageVersus(passenger, versus)));
				passenger.InflictDamage(attacker, new Damage(d, damageTypes));
			}
		}
	}

	public class RuntimeCargoInit : ValueActorInit<Actor[]>, ISuppressInitExport
	{
		public RuntimeCargoInit(TraitInfo info, Actor[] value)
			: base(info, value) { }
	}

	public class CargoInit : ValueActorInit<string[]>
	{
		public CargoInit(TraitInfo info, string[] value)
			: base(info, value) { }
	}
}
