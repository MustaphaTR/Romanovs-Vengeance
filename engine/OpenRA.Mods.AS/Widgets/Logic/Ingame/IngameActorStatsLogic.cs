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

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.AS.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class IngameActorStatsLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public IngameActorStatsLogic(Widget widget, World world, Dictionary<string, MiniYaml> logicArgs)
		{
			var selection = world.WorldActor.Trait<ISelection>();

			var largeIcons = new List<ActorIconWidget> { widget.Get<ActorIconWidget>("STAT_ICON") };
			var largeHealthBars = new List<HealthBarWidget> { widget.Get<HealthBarWidget>("STAT_HEALTH_BAR") };
			var largeIconCount = 1;
			var largeIconSpacing = new int2(2, 2);
			if (logicArgs.TryGetValue("LargeIconCount", out var largeIconCountEntry))
				largeIconCount = FieldLoader.GetValue<int>("LargeIconCount", largeIconCountEntry.Value);
			if (logicArgs.TryGetValue("LargeIconSpacing", out var largeIconSpacingEntry))
				largeIconSpacing = FieldLoader.GetValue<int2>("LargeIconSpacing", largeIconSpacingEntry.Value);
			if (largeIconCount > 1)
			{
				for (var i = 1; i < largeIconCount; i++)
				{
					var iconClone = largeIcons[0].Clone() as ActorIconWidget;
					iconClone.Bounds.X += (iconClone.IconSize.X + largeIconSpacing.X) * i;

					widget.AddChild(iconClone);
					largeIcons.Add(iconClone);

					var healthBarClone = largeHealthBars[0].Clone() as HealthBarWidget;
					healthBarClone.Bounds.X += (healthBarClone.Bounds.Width + largeIconSpacing.X) * i;

					widget.AddChild(healthBarClone);
					largeHealthBars.Add(healthBarClone);
				}
			}

			var smallIcons = new List<ActorIconWidget>();
			var smallHealthBars = new List<HealthBarWidget>();
			var smallIconCount = 0;
			var smallIconSpacing = new int2(0, 5);
			var smallIconRows = 6;
			if (logicArgs.TryGetValue("SmallIconCount", out var smallIconCountEntry))
				smallIconCount = FieldLoader.GetValue<int>("SmallIconCount", smallIconCountEntry.Value);
			if (logicArgs.TryGetValue("SmallIconSpacing", out var smallIconSpacingEntry))
				smallIconSpacing = FieldLoader.GetValue<int2>("SmallIconSpacing", smallIconSpacingEntry.Value);
			if (logicArgs.TryGetValue("SmallIconRows", out var smallIconRowsEntry))
				smallIconRows = FieldLoader.GetValue<int>("SmallIconRows", smallIconRowsEntry.Value);
			if (smallIconCount > 0)
			{
				smallIcons.Add(widget.Get<ActorIconWidget>("STAT_ICON_SMALL"));
				smallHealthBars.Add(widget.Get<HealthBarWidget>("STAT_HEALTH_BAR_SMALL"));
				for (var i = 1; i < largeIconCount + smallIconCount; i++)
				{
					var iconClone = smallIcons[0].Clone() as ActorIconWidget;
					iconClone.Bounds.X += (iconClone.IconSize.X + smallIconSpacing.X) * (i % smallIconRows);
					iconClone.Bounds.Y += (iconClone.IconSize.Y + smallIconSpacing.Y) * (i / smallIconRows);

					widget.AddChild(iconClone);
					smallIcons.Add(iconClone);

					var healthBarClone = smallHealthBars[0].Clone() as HealthBarWidget;
					healthBarClone.Bounds.X += (iconClone.IconSize.X + smallIconSpacing.X) * (i % smallIconRows);
					healthBarClone.Bounds.Y += (iconClone.IconSize.Y + smallIconSpacing.Y) * (i / smallIconRows);

					widget.AddChild(healthBarClone);
					smallHealthBars.Add(healthBarClone);
				}
			}

			var upgradeIcons = new List<ActorIconWidget> { widget.GetOrNull<ActorIconWidget>("STAT_ICON_UPGRADE") };
			if (upgradeIcons[0] != null)
			{
				var upgradeIconCount = 5;
				var upgradeIconSpacing = new int2(0, 5);

				if (logicArgs.TryGetValue("UpgradeIconCount", out var upgradeIconCountEntry))
					upgradeIconCount = FieldLoader.GetValue<int>("UpgradeIconCount", upgradeIconCountEntry.Value);
				if (logicArgs.TryGetValue("UpgradeIconSpacing", out var upgradeIconSpacingEntry))
					upgradeIconSpacing = FieldLoader.GetValue<int2>("UpgradeIconSpacing", upgradeIconSpacingEntry.Value);

				if (upgradeIconCount > 1)
				{
					for (var i = 1; i < upgradeIconCount; i++)
					{
						var iconClone = upgradeIcons[0].Clone() as ActorIconWidget;
						iconClone.Bounds.X += (iconClone.IconSize.X + upgradeIconSpacing.X) * i;

						widget.AddChild(iconClone);
						upgradeIcons.Add(iconClone);
					}
				}

				var upgIconID = 0;
				foreach (var icon in upgradeIcons)
				{
					var index = ++upgIconID;
					icon.IsVisible = () =>
					{
						var validActors = selection.Actors.Where(a => a.Info.HasTraitInfo<ActorStatValuesInfo>());
						return validActors.Count() <= 1;
					};

					icon.GetActorInfo = () =>
					{
						var validActors = selection.Actors.Where(a => a.Info.HasTraitInfo<ActorStatValuesInfo>()).ToList();
						if (validActors.Count > 1 || validActors.Count <= 0)
							return null;

						var unit = validActors[0];
						if (unit != null && !unit.IsDead)
						{
							var usv = unit.Trait<ActorStatValues>();
							if (usv.Disguised)
							{
								if (usv.DisguiseUpgrades.Count >= index)
									return unit.World.Map.Rules.Actors[usv.DisguiseCurrentUpgrades[index - 1]];

								return null;
							}
							else if (usv.Upgrades.Count >= index)
								return unit.World.Map.Rules.Actors[usv.CurrentUpgrades[index - 1]];
						}

						return null;
					};

					icon.GetDisabled = () =>
					{
						var validActors = selection.Actors.Where(a => a.Info.HasTraitInfo<ActorStatValuesInfo>()).ToList();
						if (validActors.Count > 1 || validActors.Count <= 0)
							return false;

						var unit = validActors[0];
						if (unit != null && !unit.IsDead)
						{
							var usv = unit.Trait<ActorStatValues>();
							if (usv.Disguised)
							{
								if (usv.DisguiseUpgrades.Count >= index)
									return !usv.DisguiseUpgrades[usv.DisguiseCurrentUpgrades[index - 1]];

								return false;
							}
							else if (usv.Upgrades.Count >= index)
								return !usv.Upgrades[usv.CurrentUpgrades[index - 1]];
						}

						return false;
					};
				}
			}

			var name = widget.Get<LabelWidget>("STAT_NAME");
			var more = widget.GetOrNull<LabelWidget>("STAT_MORE");

			var extraStatLabels = new List<LabelWidget>();
			var labelID = 1;
			while (widget.GetOrNull<LabelWidget>("STAT_LABEL_" + labelID.ToStringInvariant()) != null)
			{
				extraStatLabels.Add(widget.Get<LabelWidget>("STAT_LABEL_" + labelID.ToStringInvariant()));
				labelID++;
			}

			var extraStatIcons = new List<ImageWidget>();
			var iconID = 1;
			while (widget.GetOrNull<ImageWidget>("STAT_ICON_" + iconID.ToStringInvariant()) != null)
			{
				extraStatIcons.Add(widget.Get<ImageWidget>("STAT_ICON_" + iconID.ToStringInvariant()));
				iconID++;
			}

			name.GetText = () =>
			{
				var validActors = selection.Actors.Where(a => a.Info.HasTraitInfo<ActorStatValuesInfo>()).ToList();
				if (validActors.Count <= 0 || (largeIconCount > 1 && validActors.Count != 1))
					return "";

				var unit = validActors[0];
				if (unit != null && !unit.IsDead)
				{
					var usv = unit.Trait<ActorStatValues>();
					if (usv.Tooltips.Length > 0)
					{
						var stance = world.RenderPlayer == null ? PlayerRelationship.None : unit.Owner.RelationshipWith(world.RenderPlayer);
						var actorName = usv.Tooltips.FirstEnabledTraitOrDefault().TooltipInfo.TooltipForPlayerStance(stance);
						return actorName;
					}
				}

				return "";
			};

			iconID = 0;
			foreach (var icon in largeIcons)
			{
				var index = ++iconID;
				icon.IsVisible = () =>
				{
					var validActors = selection.Actors.Where(a => a.Info.HasTraitInfo<ActorStatValuesInfo>());
					if (smallIconCount > 0 && validActors.Count() > largeIconCount)
						return false;

					return index == 1 || validActors.Count() >= index;
				};

				icon.GetActor = () =>
				{
					var validActors = selection.Actors.Where(a => a.Info.HasTraitInfo<ActorStatValuesInfo>()).ToArray();
					if (validActors.Length >= index)
						return validActors[index - 1];
					else
						return null;
				};
			}

			iconID = 0;
			foreach (var icon in smallIcons)
			{
				var index = ++iconID;
				icon.IsVisible = () =>
				{
					var validActors = selection.Actors.Where(a => a.Info.HasTraitInfo<ActorStatValuesInfo>());
					return validActors.Count() > largeIconCount && validActors.Count() >= index;
				};

				icon.GetActor = () =>
				{
					var validActors = selection.Actors.Where(a => a.Info.HasTraitInfo<ActorStatValuesInfo>()).ToArray();
					if (validActors.Length >= index)
						return validActors[index - 1];
					else
						return null;
				};
			}

			if (more != null)
			{
				more.GetText = () =>
				{
					var validActors = selection.Actors.Where(a => a.Info.HasTraitInfo<ActorStatValuesInfo>());
					if (validActors.Count() <= largeIconCount + smallIconCount)
						return "";
					else
						return "+" + (validActors.Count() - (largeIconCount + smallIconCount)).ToString(NumberFormatInfo.CurrentInfo);
				};
			}

			for (var i = 0; i < largeHealthBars.Count; i++)
			{
				var index = i;
				largeHealthBars[index].IsVisible = () =>
				{
					var validActors = selection.Actors.Where(a => a.Info.HasTraitInfo<ActorStatValuesInfo>());
					if (smallIconCount > 0 && validActors.Count() > largeIconCount)
						return false;

					return index == 0 || validActors.Count() >= index + 1;
				};

				largeHealthBars[index].GetScale = () =>
				{
					var validActors = selection.Actors.Where(a => !a.IsDead && a.Info.HasTraitInfo<ActorStatValuesInfo>()).ToArray();
					if (validActors.Length >= index + 1)
					{
						var usv = validActors[index].Trait<ActorStatValues>();
						if (usv.Disguised)
							return (float)usv.DisguiseMaxHealth / usv.Health.MaxHP;

						return (float)usv.CurrentMaxHealth / usv.Health.MaxHP;
					}

					return 1f;
				};

				largeHealthBars[index].GetHealth = () =>
				{
					var validActors = selection.Actors.Where(a => !a.IsDead && a.Info.HasTraitInfo<ActorStatValuesInfo>()).ToArray();
					if (validActors.Length >= index + 1)
						return validActors[index].Trait<ActorStatValues>().Health;

					return null;
				};
			}

			for (var i = 0; i < smallHealthBars.Count; i++)
			{
				var index = i;
				smallHealthBars[index].IsVisible = () =>
				{
					var validActors = selection.Actors.Where(a => a.Info.HasTraitInfo<ActorStatValuesInfo>());
					return validActors.Count() > largeIconCount && validActors.Count() >= index + 1;
				};

				smallHealthBars[index].GetHealth = () =>
				{
					var validActors = selection.Actors.Where(a => !a.IsDead && a.Info.HasTraitInfo<ActorStatValuesInfo>()).ToArray();
					if (validActors.Length >= index + 1)
						return validActors[index].Trait<ActorStatValues>().Health;
					else
						return null;
				};
			}

			labelID = 0;
			foreach (var statLabel in extraStatLabels)
			{
				var index = ++labelID;
				statLabel.GetText = () =>
				{
					var validActors = selection.Actors.Where(a => !a.IsDead && a.Info.HasTraitInfo<ActorStatValuesInfo>()).ToList();
					if (validActors.Count <= 0 || (largeIconCount > 1 && validActors.Count > 1))
						return "";

					var unit = validActors[0];
					if (unit != null)
					{
						var usv = unit.Trait<ActorStatValues>();
						var labelText = "";
						if (usv.Disguised)
							labelText = usv.DisguiseStats[index];
						else
							labelText = usv.GetValueFor(index);

						return string.IsNullOrEmpty(labelText) ? "" : FluentProvider.GetMessage(statLabel.Text) + labelText;
					}

					return FluentProvider.GetMessage(statLabel.Text);
				};
			}

			iconID = 0;
			foreach (var statIcon in extraStatIcons)
			{
				var index = ++iconID;
				statIcon.IsVisible = () =>
				{
					var validActors = selection.Actors.Where(a => !a.IsDead && a.Info.HasTraitInfo<ActorStatValuesInfo>()).ToList();
					if (validActors.Count <= 0 || (largeIconCount > 1 && validActors.Count > 1))
						return false;

					var unit = validActors[0];
					if (unit != null)
					{
						var usv = unit.Trait<ActorStatValues>();
						if (usv.Disguised)
							return usv.DisguiseStatIcons[index] != null;

						return usv.GetIconFor(index) != null;
					}

					return true;
				};
				statIcon.GetImageName = () =>
				{
					var unit = selection.Actors.FirstOrDefault(a => a.Info.HasTraitInfo<ActorStatValuesInfo>());
					if (unit != null && !unit.IsDead)
					{
						var usv = unit.Trait<ActorStatValues>();
						var iconName = "";
						if (usv.Disguised)
							iconName = usv.DisguiseStatIcons[index];
						else
							iconName = usv.GetIconFor(index);

						return string.IsNullOrEmpty(iconName) ? statIcon.ImageName : iconName;
					}

					return statIcon.ImageName;
				};
			}
		}
	}
}
