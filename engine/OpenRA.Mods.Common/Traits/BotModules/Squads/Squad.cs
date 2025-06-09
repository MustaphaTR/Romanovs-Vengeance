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
using System.Linq;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.BotModules.Squads
{
	public enum SquadType { Assault, Air, Rush, Protection, Naval }

	public class Squad
	{
		public List<UnitWposWrapper> Units = [];
		public SquadType Type;

		internal IBot Bot;
		internal World World;
		internal SquadManagerBotModule SquadManager;
		internal MersenneTwister Random;

		internal Target Target;
		internal StateMachine FuzzyStateMachine;
		internal CPos BaseLocation;

		public Squad(IBot bot, SquadManagerBotModule squadManager, SquadType type)
			: this(bot, squadManager, type, null) { }

		public Squad(IBot bot, SquadManagerBotModule squadManager, SquadType type, Actor target)
		{
			Bot = bot;
			SquadManager = squadManager;
			World = bot.Player.PlayerActor.World;
			Random = World.LocalRandom;
			Type = type;
			Target = Target.FromActor(target);
			FuzzyStateMachine = new StateMachine();

			switch (type)
			{
				case SquadType.Assault:
					FuzzyStateMachine.ChangeState(this, new GuerrillaUnitsIdleState());
					break;
				case SquadType.Rush:
					FuzzyStateMachine.ChangeState(this, new GroundUnitsIdleState());
					break;
				case SquadType.Air:
					FuzzyStateMachine.ChangeState(this, new AirIdleState());
					break;
				case SquadType.Protection:
					FuzzyStateMachine.ChangeState(this, new UnitsForProtectionIdleState());
					break;
				case SquadType.Naval:
					FuzzyStateMachine.ChangeState(this, new NavyUnitsIdleState());
					break;
			}
		}

		public void Update()
		{
			if (IsValid)
				FuzzyStateMachine.Update(this);
		}

		public bool IsValid => Units.Count > 0;

		public Actor TargetActor
		{
			get => Target.Actor;
			set => Target = Target.FromActor(value);
		}

		public bool IsTargetValid => Target.IsValidFor(Units.FirstOrDefault().Actor);

		public bool IsTargetVisible => TargetActor.CanBeViewedByPlayer(Bot.Player);

		public WPos CenterPosition { get { return Units[0].Actor.CenterPosition; } }

		public MiniYaml Serialize()
		{
			var nodes = new List<MiniYamlNode>()
			{
				new("Type", FieldSaver.FormatValue(Type)),
				new("Units", FieldSaver.FormatValue(Units.Where(a => !SquadManager.UnitCannotBeOrdered(a.Actor)).Select(a => a.Actor.ActorID).ToArray())),
			};
			if (Target.Type == TargetType.Actor)
				nodes.Add(new MiniYamlNode("Target", FieldSaver.FormatValue(Target.Actor.ActorID)));

			return new MiniYaml("", nodes);
		}

		public static Squad Deserialize(IBot bot, SquadManagerBotModule squadManager, MiniYaml yaml)
		{
			var type = SquadType.Rush;
			Actor targetActor = null;

			var typeNode = yaml.NodeWithKeyOrDefault("Type");
			if (typeNode != null)
				type = FieldLoader.GetValue<SquadType>("Type", typeNode.Value.Value);

			var targetNode = yaml.NodeWithKeyOrDefault("Target");
			if (targetNode != null)
				targetActor = squadManager.World.GetActorById(FieldLoader.GetValue<uint>("Target", targetNode.Value.Value));

			var squad = new Squad(bot, squadManager, type, targetActor);

			var unitsNode = yaml.NodeWithKeyOrDefault("Units");
			if (unitsNode != null)
			{
				foreach (var a in FieldLoader.GetValue<uint[]>("Units", unitsNode.Value.Value)
					.Select(squadManager.World.GetActorById))
				{
					squad.Units.Add(new UnitWposWrapper(a));
				}
			}

			return squad;
		}
	}
}
