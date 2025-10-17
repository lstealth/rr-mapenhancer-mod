using System;
using Game.Messages;
using Game.State;

namespace UI.Console.Commands;

[ConsoleCommand("/mode", null)]
public class ModeCommand : IConsoleCommand
{
	public string Execute(string[] comps)
	{
		if (!StateManager.IsHost)
		{
			return "Only available on host.";
		}
		StateManager shared = StateManager.Shared;
		if (comps.Length <= 1)
		{
			return "Game Mode: " + shared.GameMode.DisplayString();
		}
		GameMode gameMode = comps[1] switch
		{
			"normal" => GameMode.Company, 
			"company" => GameMode.Company, 
			"sandbox" => GameMode.Sandbox, 
			_ => throw new Exception("Invalid game mode " + comps[1]), 
		};
		StateManager.ApplyLocal(new PropertyChange("_game", "mode", new IntPropertyValue((int)gameMode)));
		return "Set game mode: " + gameMode.DisplayString();
	}
}
