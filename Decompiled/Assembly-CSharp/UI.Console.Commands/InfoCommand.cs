using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Character;
using Game.State;
using Model;
using Model.Ops;

namespace UI.Console.Commands;

[StructLayout(LayoutKind.Sequential, Size = 1)]
[ConsoleCommand("/info", "Car information")]
public struct InfoCommand : IConsoleCommand
{
	public string Execute(string[] comps)
	{
		if (!StateManager.IsHost)
		{
			return "Only available on the host.";
		}
		if (comps.Length < 2)
		{
			return "Usage: /info <car>";
		}
		string query = comps[1];
		Car car = TrainController.Shared.CarForString(query);
		if (car == null)
		{
			return "Car not found.";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(car.CarType + " " + car.DisplayName + " " + car.id);
		if (car.IsInBardo)
		{
			stringBuilder.AppendLine("Bardo: " + car.Bardo);
		}
		else
		{
			stringBuilder.AppendLine($"{car.velocity * 2.23694f} mph");
		}
		stringBuilder.AppendLine($"Condition: {car.Condition * 100f}%");
		if (car.EnableOiling)
		{
			stringBuilder.AppendLine($"Oiled: {car.Oiled * 100f}%");
		}
		int count = car.Definition.LoadSlots.Count;
		for (int i = 0; i < count; i++)
		{
			CarLoadInfo? loadInfo = car.GetLoadInfo(i);
			if (loadInfo.HasValue)
			{
				CarLoadInfo value = loadInfo.Value;
				stringBuilder.AppendLine($"Load {i}: {value.LoadId}, {value.Quantity:N3}");
			}
		}
		OpsController shared = OpsController.Shared;
		if (shared != null)
		{
			shared.AppendCarInfo(car, stringBuilder);
		}
		return stringBuilder.ToString().Trim();
	}

	private static SpawnPoint SpawnPointFor(string spawnPointName)
	{
		return SpawnPoint.All.FirstOrDefault((SpawnPoint spawnPoint) => string.Compare(spawnPoint.name, spawnPointName, StringComparison.CurrentCultureIgnoreCase) == 0);
	}
}
