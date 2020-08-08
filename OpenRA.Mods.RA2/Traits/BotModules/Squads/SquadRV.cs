#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.RV.Traits.BotModules.Squads
{
	public enum SquadTypeRV { Assault, Air, Rush, Protection, Naval }

	public class SquadRV
	{
		public List<Actor> Units = new List<Actor>();
		public SquadTypeRV Type;

		internal IBot Bot;
		internal World World;
		internal SquadManagerBotModuleRV SquadManager;
		internal MersenneTwister Random;

		internal Target Target;
		internal StateMachineRV FuzzyStateMachine;

		public SquadRV(IBot bot, SquadManagerBotModuleRV squadManager, SquadTypeRV type)
			: this(bot, squadManager, type, null) { }

		public SquadRV(IBot bot, SquadManagerBotModuleRV squadManager, SquadTypeRV type, Actor target)
		{
			Bot = bot;
			SquadManager = squadManager;
			World = bot.Player.PlayerActor.World;
			Random = World.LocalRandom;
			Type = type;
			Target = Target.FromActor(target);
			FuzzyStateMachine = new StateMachineRV();

			switch (type)
			{
				case SquadTypeRV.Assault:
				case SquadTypeRV.Rush:
					FuzzyStateMachine.ChangeState(this, new GroundUnitsIdleStateRV(), true);
					break;
				case SquadTypeRV.Air:
					FuzzyStateMachine.ChangeState(this, new AirIdleStateRV(), true);
					break;
				case SquadTypeRV.Protection:
					FuzzyStateMachine.ChangeState(this, new UnitsForProtectionIdleStateRV(), true);
					break;
				case SquadTypeRV.Naval:
					FuzzyStateMachine.ChangeState(this, new NavyUnitsIdleStateRV(), true);
					break;
			}
		}

		public void Update()
		{
			if (IsValid)
				FuzzyStateMachine.Update(this);
		}

		public bool IsValid { get { return Units.Any(); } }

		public Actor TargetActor
		{
			get { return Target.Actor; }
			set { Target = Target.FromActor(value); }
		}

		public bool IsTargetValid
		{
			get { return Target.IsValidFor(Units.FirstOrDefault()) && !Target.Actor.Info.HasTraitInfo<HuskInfo>(); }
		}

		public bool IsTargetVisible
		{
			get { return TargetActor.CanBeViewedByPlayer(Bot.Player); }
		}

		public WPos CenterPosition { get { return Units.Select(u => u.CenterPosition).Average(); } }

		public MiniYaml Serialize()
		{
			var nodes = new MiniYaml("", new List<MiniYamlNode>()
			{
				new MiniYamlNode("Type", FieldSaver.FormatValue(Type)),
				new MiniYamlNode("Units", FieldSaver.FormatValue(Units.Select(a => a.ActorID).ToArray())),
			});

			if (Target.Type == TargetType.Actor)
				nodes.Nodes.Add(new MiniYamlNode("Target", FieldSaver.FormatValue(Target.Actor.ActorID)));

			return nodes;
		}

		public static SquadRV Deserialize(IBot bot, SquadManagerBotModuleRV squadManager, MiniYaml yaml)
		{
			var type = SquadTypeRV.Rush;
			Actor targetActor = null;

			var typeNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Type");
			if (typeNode != null)
				type = FieldLoader.GetValue<SquadTypeRV>("Type", typeNode.Value.Value);

			var targetNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Target");
			if (targetNode != null)
				targetActor = squadManager.World.GetActorById(FieldLoader.GetValue<uint>("ActiveUnits", targetNode.Value.Value));

			var squad = new SquadRV(bot, squadManager, type, targetActor);

			var unitsNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Units");
			if (unitsNode != null)
				squad.Units.AddRange(FieldLoader.GetValue<uint[]>("Units", unitsNode.Value.Value)
					.Select(a => squadManager.World.GetActorById(a)));

			return squad;
		}
	}
}
