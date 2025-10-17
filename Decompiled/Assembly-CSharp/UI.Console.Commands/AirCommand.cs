using System.Collections.Generic;
using System.Linq;
using Game.State;
using Model;

namespace UI.Console.Commands;

[ConsoleCommand("/air", null)]
public class AirCommand : IConsoleCommand
{
	private const string Usage = "/air";

	public string Execute(string[] comps)
	{
		if (!StateManager.IsHost)
		{
			return "Must be host.";
		}
		Car selectedCar = TrainController.Shared.SelectedCar;
		if (selectedCar == null)
		{
			return "No selected car.";
		}
		if (!float.TryParse(comps[1], out var _))
		{
			return "/air";
		}
		List<Car> list = selectedCar.EnumerateCoupled().ToList();
		TrainController.FillAir(list);
		return $"Filled air on {list.Count}.";
	}
}
