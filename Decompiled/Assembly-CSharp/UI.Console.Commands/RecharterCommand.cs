using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Game.State;
using Helpers;
using Model;
using Serilog;

namespace UI.Console.Commands;

[StructLayout(LayoutKind.Sequential, Size = 1)]
[ConsoleCommand("/recharter", "Change the reporting mark and name of your railroad.")]
public struct RecharterCommand : IConsoleCommand
{
	public string Execute(string[] comps)
	{
		if (!StateManager.IsHost)
		{
			return "Only available to the host.";
		}
		if (comps.Length < 3)
		{
			return "Usage: /recharter <reporting mark> \"Railroad Name\"\nExample: /recharter SOU \"Southern Railway\"";
		}
		string text = comps[1].Truncate(6);
		string text2 = comps[2].Truncate(50);
		GameStorage storage = StateManager.Shared.Storage;
		string railroadMark = storage.RailroadMark;
		storage.RailroadMark = text;
		storage.RailroadName = text2;
		TrainController shared = TrainController.Shared;
		int num = 0;
		List<Car> list = new List<Car>();
		foreach (Car car in shared.Cars)
		{
			if (!(car.Ident.ReportingMark != railroadMark) && car.IsOwnedByPlayer)
			{
				try
				{
					shared.HandleSetIdent(car.id, new CarIdent(text, car.Ident.RoadNumber));
					num++;
				}
				catch (Exception exception)
				{
					Log.Error(exception, "Recharter: Unable to set ident of car {car}", car);
					list.Add(car);
				}
			}
		}
		string text3 = $"Your railroad is now {text}, {text2}. Changed reporting marks on {num} cars.";
		if (list.Count > 0)
		{
			string arg = string.Join(", ", list.Select(Hyperlink.To));
			text3 += $"  Unable to change {list.Count} due to conflicts: {arg}";
		}
		return text3;
	}
}
