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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Implements the YR OpenTopped logic where transported actors used separate firing offsets, ignoring facing."
		+ "Compatible with both `Cargo`/`Passengers` or `Garrionable`/`Garrisoners` logic.")]
	public class AttackOpenToppedInfo : AttackFollowInfo, IRulesetLoaded
	{
		[FieldLoader.Require]
		[Desc("Fire port offsets in local coordinates.")]
		public readonly WVec[] PortOffsets = null;

		public override object Create(ActorInitializer init) { return new AttackOpenTopped(init.Self, this); }
		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (PortOffsets.Length == 0)
				throw new YamlException("PortOffsets must have at least one entry.");

			base.RulesetLoaded(rules, ai);
		}
	}

	public class AttackOpenTopped : AttackFollow, INotifyGarrisonerEntered, INotifyGarrisonerExited, IRender, INotifyPassengerEntered, INotifyPassengerExited
	{
		public new readonly AttackOpenToppedInfo Info;
		readonly Lazy<BodyOrientation> coords;
		readonly List<Actor> actors;
		readonly List<Armament> armaments;
		readonly HashSet<(AnimationWithOffset Animation, string Sequence)> muzzles;
		readonly Dictionary<Actor, IFacing> paxFacing;
		readonly Dictionary<Actor, IPositionable> paxPos;
		readonly Dictionary<Actor, RenderSprites> paxRender;

		public AttackOpenTopped(Actor self, AttackOpenToppedInfo info)
			: base(self, info)
		{
			Info = info;
			coords = Exts.Lazy(() => self.Trait<BodyOrientation>());
			actors = new List<Actor>();
			armaments = new List<Armament>();
			muzzles = new HashSet<(AnimationWithOffset Animation, string Sequence)>();
			paxFacing = new Dictionary<Actor, IFacing>();
			paxPos = new Dictionary<Actor, IPositionable>();
			paxRender = new Dictionary<Actor, RenderSprites>();
		}

		protected override Func<IEnumerable<Armament>> InitializeGetArmaments(Actor self)
		{
			return () => armaments;
		}

		void OnActorEntered(Actor enterer)
		{
			actors.Add(enterer);
			paxFacing.Add(enterer, enterer.Trait<IFacing>());
			paxPos.Add(enterer, enterer.Trait<IPositionable>());
			paxRender.Add(enterer, enterer.Trait<RenderSprites>());
			armaments.AddRange(
				enterer.TraitsImplementing<Armament>()
				.Where(a => Info.Armaments.Contains(a.Info.Name)));
		}

		void OnActorExited(Actor exiter)
		{
			actors.Remove(exiter);
			paxFacing.Remove(exiter);
			paxPos.Remove(exiter);
			paxRender.Remove(exiter);
			armaments.RemoveAll(a => a.Actor == exiter);
		}

		void INotifyGarrisonerEntered.OnGarrisonerEntered(Actor self, Actor garrisoner)
		{
			OnActorEntered(garrisoner);
		}

		void INotifyGarrisonerExited.OnGarrisonerExited(Actor self, Actor garrisoner)
		{
			OnActorExited(garrisoner);
		}

		void INotifyPassengerEntered.OnPassengerEntered(Actor self, Actor passenger)
		{
			OnActorEntered(passenger);
		}

		void INotifyPassengerExited.OnPassengerExited(Actor self, Actor passenger)
		{
			OnActorExited(passenger);
		}

		WVec SelectFirePort(Actor firer)
		{
			var passengerIndex = actors.IndexOf(firer);
			if (passengerIndex == -1)
				return new WVec(0, 0, 0);

			var portIndex = passengerIndex % Info.PortOffsets.Length;

			return Info.PortOffsets[portIndex];
		}

		WVec PortOffset(Actor self, WVec offset)
		{
			var bodyOrientation = coords.Value.QuantizeOrientation(self.Orientation);
			return coords.Value.LocalToWorld(offset.Rotate(bodyOrientation));
		}

		public override void DoAttack(Actor self, in Target target)
		{
			if (!CanAttack(self, target))
				return;

			var pos = self.CenterPosition;
			var targetedPosition = GetTargetPosition(pos, target);
			var targetYaw = (targetedPosition - pos).Yaw;

			foreach (var a in Armaments)
			{
				if (a.IsTraitDisabled)
					continue;

				var port = SelectFirePort(a.Actor);

				var muzzleFacing = targetYaw;
				paxFacing[a.Actor].Facing = muzzleFacing;
				paxPos[a.Actor].SetCenterPosition(a.Actor, pos + PortOffset(self, port));

				if (!a.CheckFire(a.Actor, facing, target))
					continue;

				if (a.Info.MuzzleSequence != null)
				{
					// Muzzle facing is fixed once the firing starts
					var muzzleAnim = new Animation(self.World, paxRender[a.Actor].GetImage(a.Actor), () => targetYaw);
					var sequence = a.Info.MuzzleSequence;
					var palette = a.Info.MuzzlePalette;

					var muzzleFlash = new AnimationWithOffset(muzzleAnim,
						() => PortOffset(self, port),
						() => false,
						p => RenderUtils.ZOffsetFromCenter(self, p, 1024));

					var pair = (muzzleFlash, palette);
					muzzles.Add(pair);
					muzzleAnim.PlayThen(sequence, () => muzzles.Remove(pair));
				}

				foreach (var npa in self.TraitsImplementing<INotifyAttack>())
					npa.Attacking(self, target, a, null);
			}
		}

		IEnumerable<IRenderable> IRender.Render(Actor self, WorldRenderer wr)
		{
			// Display muzzle flashes
			foreach (var m in muzzles)
				foreach (var r in m.Animation.Render(self, wr.Palette(m.Sequence)))
					yield return r;
		}

		IEnumerable<Rectangle> IRender.ScreenBounds(Actor self, WorldRenderer wr)
		{
			// Muzzle flashes don't contribute to actor bounds
			yield break;
		}

		protected override void Tick(Actor self)
		{
			base.Tick(self);

			// Take a copy so that Tick() can remove animations
			foreach (var m in muzzles.ToArray())
				m.Animation.Animation.Tick();
		}
	}
}
