using System.Collections.Generic;
using System.Linq;
using Core;
using Game.State;
using Model;

namespace UI.Console.Commands;

[ConsoleCommand("/speed", null)]
public class SpeedCommand : IConsoleCommand
{
	private const string Usage = "/speed <mph>";

	public string Execute(string[] comps)
	{
		if (comps.Length < 2)
		{
			return "/speed <mph>";
		}
		if (!StateManager.IsHost)
		{
			return "Must be host.";
		}
		Car selectedCar = TrainController.Shared.SelectedCar;
		if (selectedCar == null)
		{
			return "No selected car.";
		}
		if (!float.TryParse(comps[1], out var result))
		{
			return "/speed <mph>";
		}
		float num = result * 0.44703928f;
		List<Car> list = selectedCar.EnumerateCoupled().ToList();
		selectedCar.set.SetVelocity(num * selectedCar.Orientation, list);
		selectedCar.ResetAtRest();
		return string.Format("Set speed on {0} to {1:F1}", list.Count.Pluralize("car"), result);
	}
}
