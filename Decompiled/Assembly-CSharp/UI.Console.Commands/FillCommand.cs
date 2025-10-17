using System;
using System.Linq;
using System.Runtime.InteropServices;
using Game.State;
using Model;
using Model.Definition.Data;
using Model.Ops;

namespace UI.Console.Commands;

[StructLayout(LayoutKind.Sequential, Size = 1)]
[ConsoleCommand("/fill", "Fill car")]
public struct FillCommand : IConsoleCommand
{
	public string Execute(string[] comps)
	{
		if (comps.Length < 2)
		{
			return "Usage: /fill <car>";
		}
		if (StateManager.Shared.GameMode != GameMode.Sandbox)
		{
			return "Only available in Sandbox.";
		}
		string query = comps[1];
		float num = ((comps.Length >= 3) ? float.Parse(comps[2]) : 1f);
		Car car = TrainController.Shared.CarForString(query);
		if (car == null)
		{
			return "Car not found.";
		}
		if (car.Definition.LoadSlots.Any((LoadSlot sd) => string.IsNullOrEmpty(sd.RequiredLoadIdentifier)))
		{
			throw new Exception("Only for use with equipment with required loads (tenders, diesels, pulpwood cars).");
		}
		int count = car.Definition.LoadSlots.Count;
		for (int num2 = 0; num2 < count; num2++)
		{
			LoadSlot loadSlot = car.Definition.LoadSlots[num2];
			car.SetLoadInfo(num2, new CarLoadInfo(loadSlot.RequiredLoadIdentifier, num * loadSlot.MaximumCapacity));
		}
		return $"Fill to {num:P}";
	}
}
