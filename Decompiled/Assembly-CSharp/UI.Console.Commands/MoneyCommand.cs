using Game.State;

namespace UI.Console.Commands;

[ConsoleCommand("/money", null)]
public class MoneyCommand : IConsoleCommand
{
	public string Execute(string[] comps)
	{
		StateManager shared = StateManager.Shared;
		if (comps.Length == 3 && comps[1] == "cheat" && int.TryParse(comps[2], out var result))
		{
			if (StateManager.Shared.GameMode != GameMode.Sandbox)
			{
				return "Only available in Sandbox.";
			}
			shared.ApplyToBalance(result, Ledger.Category.Bank, null, "Manual Adjustment");
			return "Applied.";
		}
		int balance = shared.GetBalance();
		return $"Balance: {balance:C0}";
	}
}
