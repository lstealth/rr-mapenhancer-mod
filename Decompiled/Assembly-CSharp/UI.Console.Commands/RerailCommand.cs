using System.Linq;
using Game.Messages;
using Game.State;
using Model;

namespace UI.Console.Commands;

[ConsoleCommand("/rerail", null)]
public class RerailCommand : IConsoleCommand
{
	public string Execute(string[] comps)
	{
		Car selectedCar = TrainController.Shared.SelectedCar;
		if (selectedCar == null)
		{
			return "No selected car.";
		}
		StateManager.ApplyLocal(new Rerail((from c in selectedCar.EnumerateCoupled()
			select c.id).ToArray(), 1f));
		return null;
	}
}
