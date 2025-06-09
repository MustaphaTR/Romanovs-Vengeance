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

using OpenRA.Mods.Common.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Grants Condition on subterranean layer. Also plays transition audio-visuals.")]
	public class GrantConditionOnSubterraneanLayerInfo : GrantConditionOnLayerInfo
	{
		[GrantedConditionReference]
		[Desc("The condition to grant to self when getting out of the subterranean layer.")]
		public readonly string ResurfaceCondition = null;

		[Desc("How long to grant ResurfaceCondition for.")]
		public readonly int ResurfaceConditionDuration = 25;

		[Desc("Dig animation image to play when transitioning.")]
		public readonly string SubterraneanTransitionImage = null;

		[SequenceReference(nameof(SubterraneanTransitionImage))]
		[Desc("Dig animation sequence to play when transitioning.")]
		public readonly string SubterraneanTransitionSequence = null;

		[PaletteReference]
		public readonly string SubterraneanTransitionPalette = "effect";

		[Desc("Dig sound to play when transitioning.")]
		public readonly string SubterraneanTransitionSound = null;

		[Desc("Do the sound play under shroud or fog.")]
		public readonly bool AudibleThroughFog = false;

		[Desc("Ignore fog checks for following relationships.")]
		public readonly PlayerRelationship AlwaysPlayFor = PlayerRelationship.Ally;

		[Desc("Volume the SubterraneanTransitionSound played at.")]
		public readonly float SoundVolume = 1f;

		public readonly bool ShowSelectionBar = true;
		public readonly Color SelectionBarColor = Color.Red;

		public override object Create(ActorInitializer init) { return new GrantConditionOnSubterraneanLayer(this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			var mobileInfo = ai.TraitInfoOrDefault<MobileInfo>();
			if (mobileInfo == null || mobileInfo.LocomotorInfo is not SubterraneanLocomotorInfo)
				throw new YamlException("GrantConditionOnSubterraneanLayer requires Mobile to be linked to a SubterraneanLocomotor!");

			base.RulesetLoaded(rules, ai);
		}
	}

	public class GrantConditionOnSubterraneanLayer : GrantConditionOnLayer<GrantConditionOnSubterraneanLayerInfo>, INotifyCenterPositionChanged,
		ITick, ISync, ISelectionBar
	{
		WDist transitionDepth;
		protected int resurfaceConditionToken = Actor.InvalidConditionToken;

		[Sync]
		int resurfaceTicks;

		public GrantConditionOnSubterraneanLayer(GrantConditionOnSubterraneanLayerInfo info)
			: base(info, CustomMovementLayerType.Subterranean) { }

		protected override void Created(Actor self)
		{
			var mobileInfo = self.Info.TraitInfo<MobileInfo>();
			var li = (SubterraneanLocomotorInfo)mobileInfo.LocomotorInfo;
			transitionDepth = li.SubterraneanTransitionDepth;
			base.Created(self);
		}

		void ITick.Tick(Actor self)
		{
			if (--resurfaceTicks <= 0 && resurfaceConditionToken != Actor.InvalidConditionToken)
				resurfaceConditionToken = self.RevokeCondition(resurfaceConditionToken);
		}

		void PlayTransitionAudioVisuals(Actor self, CPos fromCell)
		{
			if (!string.IsNullOrEmpty(Info.SubterraneanTransitionSequence))
				self.World.AddFrameEndTask(w => w.Add(new SpriteEffect(self.World.Map.CenterOfCell(fromCell), self.World,
					Info.SubterraneanTransitionImage,
					Info.SubterraneanTransitionSequence, Info.SubterraneanTransitionPalette)));

			var pos = self.CenterPosition;
			var viewver = self.World.RenderPlayer ?? self.World.LocalPlayer;
			if (!string.IsNullOrEmpty(Info.SubterraneanTransitionSound) &&
				(Info.AudibleThroughFog || viewver == null || Info.AlwaysPlayFor.HasRelationship(viewver.RelationshipWith(self.Owner)) ||
				(!self.World.ShroudObscures(pos) && !self.World.FogObscures(pos))))
				Game.Sound.Play(SoundType.World, Info.SubterraneanTransitionSound, pos, Info.SoundVolume);
		}

		void INotifyCenterPositionChanged.CenterPositionChanged(Actor self, byte oldLayer, byte newLayer)
		{
			var depth = self.World.Map.DistanceAboveTerrain(self.CenterPosition);

			// Grant condition when new layer is Subterranean and depth is lower than transition depth,
			// revoke condition when new layer is not Subterranean and depth is at or higher than transition depth.
			if (newLayer == ValidLayerType && depth < transitionDepth && conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(Info.Condition);
			else if (newLayer != ValidLayerType && depth > transitionDepth && conditionToken != Actor.InvalidConditionToken)
			{
				conditionToken = self.RevokeCondition(conditionToken);
				PlayTransitionAudioVisuals(self, self.Location);

				if (resurfaceConditionToken == Actor.InvalidConditionToken)
					resurfaceConditionToken = self.GrantCondition(Info.ResurfaceCondition);

				resurfaceTicks = Info.ResurfaceConditionDuration;
			}
		}

		protected override void UpdateConditions(Actor self, byte oldLayer, byte newLayer)
		{
			// Special case, only audio-visuals are played at the time the Layer changes from normal to Subterranean
			if (newLayer == ValidLayerType && oldLayer != ValidLayerType)
				PlayTransitionAudioVisuals(self, self.Location);
		}

		float ISelectionBar.GetValue()
		{
			if (IsTraitDisabled || !Info.ShowSelectionBar || resurfaceTicks <= 0)
				return 0f;

			return (float)resurfaceTicks / Info.ResurfaceConditionDuration;
		}

		bool ISelectionBar.DisplayWhenEmpty { get { return false; } }

		Color ISelectionBar.GetColor() { return Info.SelectionBarColor; }
	}
}
