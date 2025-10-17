using System.Text.RegularExpressions;
using Game.Messages;
using Game.State;

namespace UI.Console.Commands;

[ConsoleCommand("/wait", "Wait for a number of game hours.")]
public class WaitCommand : IConsoleCommand
{
	public string Execute(string[] comps)
	{
		if (comps.Length < 2 || !TryParseHours(comps[1], out var hours))
		{
			return "Usage: /wait <hours|hh:mm>";
		}
		StateManager.ApplyLocal(new WaitTime
		{
			Hours = hours
		});
		return null;
	}

	public static bool TryParseHours(string str, out float hours)
	{
		if (float.TryParse(str, out var result))
		{
			hours = result;
			return true;
		}
		Match match = Regex.Match(str, "^(\\d+):(\\d{2})$");
		if (!match.Success)
		{
			hours = 0f;
			return false;
		}
		string value = match.Groups[1].Value;
		string value2 = match.Groups[2].Value;
		if (!int.TryParse(value, out var result2) || !int.TryParse(value2, out var result3))
		{
			hours = 0f;
			return false;
		}
		hours = (float)result2 + (float)result3 / 60f;
		return true;
	}
}
