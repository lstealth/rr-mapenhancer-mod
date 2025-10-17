using Game.Progression;
using Game.State;
using UnityEngine;

namespace UI.Console.Commands;

[ConsoleCommand("/mapfeature", null)]
public class MapFeatureCommand : IConsoleCommand
{
	private const string Usage = "Usage: /mapfeature <feature> <true|false>";

	public string Execute(string[] comps)
	{
		if (comps.Length < 3)
		{
			return "Usage: /mapfeature <feature> <true|false>";
		}
		if (StateManager.Shared.GameMode != GameMode.Sandbox)
		{
			return "Only available in Sandbox.";
		}
		Object.FindObjectOfType<MapFeatureManager>().SetFeatureEnabled(comps[1], comps[2] == "true");
		return "Set.";
	}
}
