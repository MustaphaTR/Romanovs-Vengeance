#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.AS.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("This actor can store Garrisoner actors.")]
	public class GarrisonableInfo : PausableConditionalTraitInfo, Requires<IOccupySpaceInfo>
	{
		[Desc("The maximum sum of Garrisoner.Weight that this actor can support.")]
		public readonly int MaxWeight = 0;

		[Desc("`Garrisoner.GarrisonType`s that can be loaded into this actor.")]
		public readonly HashSet<string> Types = new();

		[Desc("A list of actor types that are initially spawned into this actor.")]
		public readonly string[] InitialUnits = Array.Empty<string>();

		[Desc("When this actor is sold should all of its garrisoners be unloaded?")]
		public readonly bool EjectOnSell = true;

		[Desc("When this actor dies should all of its garrisoners be unloaded?")]
		public readonly bool EjectOnDeath = false;

		[Desc("Terrain types that this actor is allowed to eject actors onto. Leave empty for all terrain types.")]
		public readonly HashSet<string> UnloadTerrainTypes = new();

		[VoiceReference]
		[Desc("Voice to play when ordered to unload the garrisoners.")]
		public readonly string UnloadVoice = "Action";

		[Desc("Radius to search for a load/unload location if the ordered cell is blocked.")]
		public readonly WDist LoadRange = WDist.FromCells(5);

		[Desc("Which direction the garrisoner will face (relative to the transport) when unloading.")]
		public readonly int GarrisonerFacing = 128;

		[Desc("Delay (in ticks) before continuing after loading a passenger.")]
		public readonly int AfterLoadDelay = 8;

		[Desc("Delay (in ticks) before unloading the first passenger.")]
		public readonly int BeforeUnloadDelay = 8;

		[Desc("Delay (in ticks) before continuing after unloading a passenger.")]
		public readonly int AfterUnloadDelay = 25;

		[Desc("Cursor to display when able to unload the garrisoners.")]
		public readonly string UnloadCursor = "deploy";

		[Desc("Cursor to display when unable to unload the garrisoners.")]
		public readonly string UnloadBlockedCursor = "deploy-blocked";

		[GrantedConditionReference]
		[Desc("The condition to grant to self while waiting for garrisonable to load.")]
		public readonly string LoadingCondition = null;

		[GrantedConditionReference]
		[Desc("The condition to grant to self while garrisoners are loaded.",
			"Condition can stack with multiple garrisoners.")]
		public readonly string LoadedCondition = null;

		[Desc("Conditions to grant when specified actors are loaded inside the transport.",
			"A dictionary of [actor id]: [condition].")]
		public readonly Dictionary<string, string> GarrisonerConditions = new();

		[GrantedConditionReference]
		public IEnumerable<string> LinterGarrisonerConditions { get { return GarrisonerConditions.Values; } }

		[Desc("Change the passengers owner if transport owner changed")]
		public readonly bool OwnerChangedAffectsGarrisoners = true;

		public override object Create(ActorInitializer init) { return new Garrisonable(init, this); }
	}

	public class Garrisonable : PausableConditionalTrait<GarrisonableInfo>, IIssueOrder, IResolveOrder, IOrderVoice,
		INotifyKilled, INotifyOwnerChanged, INotifySold, INotifyActorDisposing, IIssueDeployOrder,
		ITransformActorInitModifier, INotifyPassengersDamage
	{
		readonly Actor self;
		readonly List<Actor> garrisonable = new();
		readonly HashSet<Actor> reserves = new();
		readonly Dictionary<string, Stack<int>> garrisonerTokens = new();
		readonly Lazy<IFacing> facing;
		readonly bool checkTerrainType;
		readonly Stack<int> loadedTokens = new();

		public int TotalWeight = 0;
		int reservedWeight = 0;
		Aircraft aircraft;
		int loadingToken = Actor.InvalidConditionToken;
		bool takeOffAfterLoad;
		bool initialised;

		public IEnumerable<Actor> Garrisoners { get { return garrisonable; } }
		public int GarrisonerCount { get { return garrisonable.Count; } }

		enum State { Free, Locked }
		State state = State.Free;

		public Garrisonable(ActorInitializer init, GarrisonableInfo info)
			: base(info)
		{
			self = init.Self;
			checkTerrainType = info.UnloadTerrainTypes.Count > 0;

			var runtimeGarrisonInit = init.GetOrDefault<RuntimeGarrisonInit>(info);
			var garrisonInit = init.GetOrDefault<GarrisonInit>(info);
			if (runtimeGarrisonInit != null)
			{
				garrisonable = runtimeGarrisonInit.Value.ToList();
				TotalWeight = garrisonable.Sum(c => GetWeight(c));
			}
			else if (garrisonInit != null)
			{
				foreach (var u in garrisonInit.Value)
				{
					var unit = self.World.CreateActor(false, u.ToLowerInvariant(),
						new TypeDictionary { new OwnerInit(self.Owner) });

					garrisonable.Add(unit);
				}

				TotalWeight = garrisonable.Sum(c => GetWeight(c));
			}
			else
			{
				foreach (var u in info.InitialUnits)
				{
					var unit = self.World.CreateActor(false, u.ToLowerInvariant(),
						new TypeDictionary { new OwnerInit(self.Owner) });

					garrisonable.Add(unit);
				}

				TotalWeight = garrisonable.Sum(c => GetWeight(c));
			}

			facing = Exts.Lazy(self.TraitOrDefault<IFacing>);
		}

		protected override void Created(Actor self)
		{
			base.Created(self);

			aircraft = self.TraitOrDefault<Aircraft>();

			if (garrisonable.Count > 0)
			{
				foreach (var c in garrisonable)
				{
					if (Info.GarrisonerConditions.TryGetValue(c.Info.Name, out var garrisonerCondition))
						garrisonerTokens.GetOrAdd(c.Info.Name).Push(self.GrantCondition(garrisonerCondition));
				}

				if (!string.IsNullOrEmpty(Info.LoadedCondition))
					loadedTokens.Push(self.GrantCondition(Info.LoadedCondition));
			}

			// Defer notifications until we are certain all traits on the transport are initialised
			self.World.AddFrameEndTask(w =>
			{
				foreach (var c in garrisonable)
				{
					c.Trait<Passenger>().Transport = self;

					foreach (var nec in c.TraitsImplementing<INotifyEnteredGarrison>())
						nec.OnEnteredGarrison(c, self);

					foreach (var npe in self.TraitsImplementing<INotifyGarrisonerEntered>())
						npe.OnGarrisonerEntered(self, c);
				}

				initialised = true;
			});
		}

		static int GetWeight(Actor a) { return a.Info.TraitInfo<GarrisonerInfo>().Weight; }

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
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

				self.QueueActivity(new UnloadGarrison(self, Info.LoadRange));
			}
		}

		public IEnumerable<CPos> CurrentAdjacentCells()
		{
			return Util.AdjacentCells(self.World, Target.FromActor(self)).Where(c => self.Location != c);
		}

		public bool CanUnload(BlockedByActor check = BlockedByActor.None)
		{
			if (checkTerrainType)
			{
				if (!self.World.Map.Contains(self.Location))
					return false;

				if (!Info.UnloadTerrainTypes.Contains(self.World.Map.GetTerrainInfo(self.Location).Type))
					return false;
			}

			return !IsEmpty() && (aircraft == null || aircraft.CanLand(self.Location, blockedByMobile: false))
				&& CurrentAdjacentCells().Any(c => Garrisoners.Any(p => !p.IsDead && p.Trait<IPositionable>().CanEnterCell(c, null, check)));
		}

		public bool CanLoad(Actor a)
		{
			return reserves.Contains(a) || HasSpace(GetWeight(a));
		}

		internal bool ReserveSpace(Actor a)
		{
			if (reserves.Contains(a))
				return true;

			var w = GetWeight(a);
			if (!HasSpace(w))
				return false;

			if (loadingToken == Actor.InvalidConditionToken && !string.IsNullOrEmpty(Info.LoadingCondition))
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

		public bool HasSpace(int weight) { return TotalWeight + reservedWeight + weight <= Info.MaxWeight; }
		public bool IsEmpty() { return garrisonable.Count == 0; }

		public Actor Peek() { return garrisonable.Last(); }

		public Actor Unload(Actor self, Actor passenger = null)
		{
			passenger ??= garrisonable.Last();
			if (!garrisonable.Remove(passenger))
				throw new ArgumentException("Attempted to ungarrison an actor that is not a garrisoner.");

			TotalWeight -= GetWeight(passenger);

			SetGarrisonerFacing(passenger);

			foreach (var npe in self.TraitsImplementing<INotifyGarrisonerExited>())
				npe.OnGarrisonerExited(self, passenger);

			foreach (var nec in passenger.TraitsImplementing<INotifyExitedGarrison>())
				nec.OnExitedGarrison(passenger, self);

			var p = passenger.Trait<Garrisoner>();
			p.Transport = null;

			if (garrisonerTokens.TryGetValue(passenger.Info.Name, out var garrisonerToken) && garrisonerToken.Count > 0)
				self.RevokeCondition(garrisonerToken.Pop());

			if (loadedTokens.Count > 0)
				self.RevokeCondition(loadedTokens.Pop());

			return passenger;
		}

		void SetGarrisonerFacing(Actor garrisoner)
		{
			if (facing.Value == null)
				return;

			var garrisonerFacing = garrisoner.TraitOrDefault<IFacing>();
			if (garrisonerFacing != null)
				garrisonerFacing.Facing = facing.Value.Facing + WAngle.FromFacing(Info.GarrisonerFacing);
		}

		public void Load(Actor self, Actor a)
		{
			garrisonable.Add(a);
			var w = GetWeight(a);
			TotalWeight += w;
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
				a.Trait<Garrisoner>().Transport = self;

				foreach (var nec in a.TraitsImplementing<INotifyEnteredGarrison>())
					nec.OnEnteredGarrison(a, self);

				foreach (var npe in self.TraitsImplementing<INotifyGarrisonerEntered>())
					npe.OnGarrisonerEntered(self, a);
			}

			if (Info.GarrisonerConditions.TryGetValue(a.Info.Name, out var garrisonerCondition))
				garrisonerTokens.GetOrAdd(a.Info.Name).Push(self.GrantCondition(garrisonerCondition));

			if (!string.IsNullOrEmpty(Info.LoadedCondition))
				loadedTokens.Push(self.GrantCondition(Info.LoadedCondition));
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (Info.EjectOnDeath)
				while (!IsEmpty() && CanUnload(BlockedByActor.All))
				{
					var garrisoner = Unload(self);
					var cp = self.CenterPosition;
					var inAir = self.World.Map.DistanceAboveTerrain(cp).Length != 0;
					var positionable = garrisoner.Trait<IPositionable>();
					positionable.SetPosition(garrisoner, self.Location);

					if (!inAir && positionable.CanEnterCell(self.Location, self, BlockedByActor.None))
					{
						self.World.AddFrameEndTask(w => w.Add(garrisoner));
						var nbms = garrisoner.TraitsImplementing<INotifyBlockingMove>();
						foreach (var nbm in nbms)
							nbm.OnNotifyBlockingMove(garrisoner, garrisoner);
					}
					else
						garrisoner.Kill(e.Attacker);
				}

			foreach (var c in garrisonable)
				c.Kill(e.Attacker);

			garrisonable.Clear();
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			foreach (var c in garrisonable)
				c.Dispose();

			garrisonable.Clear();
		}

		void INotifySold.Selling(Actor self) { }
		void INotifySold.Sold(Actor self)
		{
			if (!Info.EjectOnSell || garrisonable == null)
				return;

			while (!IsEmpty())
				SpawnGarrisoner(Unload(self));
		}

		void SpawnGarrisoner(Actor garrisoner)
		{
			self.World.AddFrameEndTask(w =>
			{
				w.Add(garrisoner);
				garrisoner.Trait<IPositionable>().SetPosition(garrisoner, self.Location);

				// TODO: this won't work well for >1 actor as they should move towards the next enterable (sub) cell instead
			});
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			if (!Info.OwnerChangedAffectsGarrisoners || garrisonable == null)
				return;

			foreach (var p in Garrisoners)
				p.ChangeOwner(newOwner);
		}

		void ITransformActorInitModifier.ModifyTransformActorInit(Actor self, TypeDictionary init)
		{
			init.Add(new RuntimeGarrisonInit(Info, Garrisoners.ToArray()));
		}

		protected override void TraitDisabled(Actor self)
		{
			if (!CanUnload())
				return;

			self.CancelActivity();
			self.QueueActivity(new UnloadGarrison(self, Info.LoadRange));
		}

		public int DamageVersus(Actor victim, Dictionary<string, int> versus)
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
			var passengersToDamage = amount > 0 && amount < garrisonable.Count ? garrisonable.Shuffle(self.World.SharedRandom).Take(amount) : garrisonable;
			foreach (var passenger in passengersToDamage)
			{
				var d = Util.ApplyPercentageModifiers(damage, damageModifiers.Append(DamageVersus(passenger, versus)));
				passenger.InflictDamage(attacker, new Damage(d, damageTypes));
			}
		}
	}

	public class RuntimeGarrisonInit : ValueActorInit<Actor[]>, ISuppressInitExport
	{
		public RuntimeGarrisonInit(TraitInfo info, Actor[] value)
			: base(info, value) { }
	}

	public class GarrisonInit : ValueActorInit<string[]>
	{
		public GarrisonInit(TraitInfo info, string[] value)
			: base(info, value) { }
	}
}
