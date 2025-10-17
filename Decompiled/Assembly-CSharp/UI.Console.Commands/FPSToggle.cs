using UnityEngine;

namespace UI.Console.Commands;

[ConsoleCommand("/stats", "Toggle stats display.")]
public class FPSToggle : IConsoleCommand
{
	public string Execute(string[] comps)
	{
		if (comps.Length < 2)
		{
			return "Usage: /stats <off|ms|fps>";
		}
		FPSDisplay fPSDisplay = Object.FindObjectOfType<FPSDisplay>();
		if (fPSDisplay == null)
		{
			return "Couldn't find FPSDisplay";
		}
		string text = comps[1];
		switch (text)
		{
		case "off":
			fPSDisplay.Run = false;
			break;
		case "fps":
			fPSDisplay.Run = true;
			fPSDisplay.displayMode = FPSDisplay.DisplayMode.FPS;
			break;
		case "ms":
			fPSDisplay.Run = true;
			fPSDisplay.displayMode = FPSDisplay.DisplayMode.MS;
			break;
		default:
			return "Unrecognized stats mode.";
		}
		return "Stats: " + text;
	}
}
