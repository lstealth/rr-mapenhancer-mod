using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Character;
using Model;

namespace UI.Console.Commands;

[StructLayout(LayoutKind.Sequential, Size = 1)]
[ConsoleCommand("/tp", "Teleport")]
public struct TeleportCommand : IConsoleCommand
{
	public string Execute(string[] comps)
	{
		if (comps.Length < 2)
		{
			IEnumerable<string> values = from sp in SpawnPoint.All
				orderby sp.name
				select sp.name.ToLower();
			return "Usage: /tp <place>\nPlaces: " + string.Join(", ", values);
		}
		string text = comps[1];
		CameraSelector shared = CameraSelector.shared;
		SpawnPoint spawnPoint = SpawnPointFor(text);
		if (spawnPoint != null)
		{
			var (gamePoint, rotation) = spawnPoint.GamePositionRotation;
			shared.JumpToPoint(gamePoint, rotation);
			return "Jump to " + spawnPoint.name;
		}
		Car car = TrainController.Shared.CarForString(text);
		if (car != null)
		{
			shared.FollowCar(car);
			return "Follow " + car.DisplayName;
		}
		return "Couldn't find destination: " + text;
	}

	private static SpawnPoint SpawnPointFor(string spawnPointName)
	{
		return SpawnPoint.All.FirstOrDefault((SpawnPoint spawnPoint) => string.Compare(spawnPoint.name, spawnPointName, StringComparison.CurrentCultureIgnoreCase) == 0);
	}
}
