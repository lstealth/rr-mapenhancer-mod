using Game.State;

namespace UI.Console.Commands;

[ConsoleCommand("/savegame", null)]
public class SaveGameCommand : IConsoleCommand
{
	public string Execute(string[] comps)
	{
		if (!StateManager.IsHost)
		{
			return "Only available on host.";
		}
		string text = SaveNameFromComponents(comps);
		SaveManager.Shared.Save(text);
		return "Saved: " + text;
	}

	internal static string SaveNameFromComponents(string[] comps)
	{
		return string.Join(" ", comps, 1, comps.Length - 1);
	}
}
