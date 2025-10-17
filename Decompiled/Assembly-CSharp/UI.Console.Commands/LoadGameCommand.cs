using System.Collections.Generic;
using Game.Persistence;
using Game.State;

namespace UI.Console.Commands;

[ConsoleCommand("/loadgame", null)]
public class LoadGameCommand : IConsoleCommand
{
	public string Execute(string[] comps)
	{
		if (!StateManager.IsHost)
		{
			return "Only available on host.";
		}
		if (comps.Length <= 1)
		{
			List<WorldStore.SaveInfo> list = WorldStore.FindSaveInfos();
			if (list.Count == 0)
			{
				return "No saves.";
			}
			return $"{list.Count} saves:\n" + string.Join("\n", list);
		}
		string saveName = SaveGameCommand.SaveNameFromComponents(comps);
		SaveManager.Shared.Load(saveName);
		return "Loaded!";
	}
}
