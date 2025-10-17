using System.Collections.Generic;
using Game;
using Game.Messages;
using Game.State;

namespace UI.Console.Commands;

[ConsoleCommand("/weather", null)]
public class WeatherCommand : IConsoleCommand
{
	public string Execute(string[] comps)
	{
		Dictionary<string, int> weatherIdLookup = TimeWeather.WeatherIdLookup;
		string result = "Usage: /weather <" + string.Join("|", weatherIdLookup.Keys) + ">";
		if (comps.Length < 2)
		{
			return result;
		}
		string key = comps[1];
		if (!weatherIdLookup.ContainsKey(key))
		{
			return result;
		}
		int value = weatherIdLookup[key];
		StateManager.ApplyLocal(new PropertyChange("_game", "weatherId", new IntPropertyValue(value)));
		return null;
	}
}
