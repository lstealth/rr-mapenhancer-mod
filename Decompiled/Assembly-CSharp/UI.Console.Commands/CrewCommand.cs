using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.Messages;
using Game.State;

namespace UI.Console.Commands;

[ConsoleCommand("/crew", "Train crew management.")]
public class CrewCommand : IConsoleCommand
{
	public string Execute(string[] comps)
	{
		if (comps.Length < 2)
		{
			return "Usage: /crew list|create|delete|join|leave ...";
		}
		switch (comps[1])
		{
		case "list":
			return List();
		case "create":
		{
			string name4 = Suffix(comps, 2);
			return Create(name4);
		}
		case "delete":
		{
			string name3 = Suffix(comps, 2);
			return Delete(name3);
		}
		case "join":
		{
			string name2 = Suffix(comps, 2);
			return JoinLeave(name2, join: true);
		}
		case "leave":
		{
			string name = Suffix(comps, 2);
			return JoinLeave(name, join: false);
		}
		default:
			return "Usage: /crew list|create|delete|join|leave ...";
		}
	}

	private string Suffix(string[] comps, int offset)
	{
		if (comps.Length < offset)
		{
			throw new Exception("Incorrect usage.");
		}
		return string.Join(" ", new ArraySegment<string>(comps, offset, comps.Length - offset));
	}

	private string Create(string name)
	{
		StateManager.ApplyLocal(new RequestCreateTrainCrew(new Snapshot.TrainCrew("", name, new HashSet<string> { PlayersManager.PlayerId.String }, "", null)));
		return "Requested create crew.";
	}

	private string Delete(string name)
	{
		StateManager.ApplyLocal(new RequestDeleteTrainCrew(TrainCrewForName(name).Id));
		return "Requested create crew.";
	}

	private string JoinLeave(string name, bool join)
	{
		TrainCrew trainCrew = TrainCrewForName(name);
		StateManager.ApplyLocal(new RequestSetTrainCrewMembership(PlayersManager.PlayerId.String, trainCrew.Id, join));
		return "Requested join.";
	}

	private static TrainCrew TrainCrewForName(string name)
	{
		return StateManager.Shared.PlayersManager.TrainCrews.First((TrainCrew trainCrew) => trainCrew.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
	}

	private string List()
	{
		PlayersManager playersManager = StateManager.Shared.PlayersManager;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"{playersManager.TrainCrews.Count()} Train Crews");
		stringBuilder.Append(string.Join("\n", playersManager.TrainCrews.Select((TrainCrew crew) => crew.Name + ": " + string.Join(", ", crew.MemberPlayerIds))));
		return stringBuilder.ToString();
	}
}
