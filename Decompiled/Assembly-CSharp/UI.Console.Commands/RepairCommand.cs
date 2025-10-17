using Core;
using Game.State;
using Model;
using UnityEngine;

namespace UI.Console.Commands;

[ConsoleCommand("/repair", null)]
public class RepairCommand : IConsoleCommand
{
	public string Execute(string[] comps)
	{
		if (!StateManager.IsHost || !StateManager.IsSandbox)
		{
			return "Only available to the host in sandbox mode.";
		}
		Car selectedCar = TrainController.Shared.SelectedCar;
		if (selectedCar == null)
		{
			return "No selected car.";
		}
		int num = 0;
		foreach (Car item in selectedCar.EnumerateCoupled())
		{
			if (!(Mathf.Abs(item.Condition) > 0.999f))
			{
				item.SetCondition(1f);
				num++;
			}
		}
		return "Repaired " + num.Pluralize("car") + ".";
	}
}
