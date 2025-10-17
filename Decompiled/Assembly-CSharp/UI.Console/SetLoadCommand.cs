using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Core;
using Game.State;
using Model;
using Model.Definition;
using Model.Definition.Data;
using Model.Ops;
using Model.Ops.Definition;

namespace UI.Console;

[StructLayout(LayoutKind.Sequential, Size = 1)]
[ConsoleCommand("/setload", "Set car load")]
public struct SetLoadCommand : IConsoleCommand
{
	private const string Usage = "Usage: /setload <car|*> <identifier|empty> [lbs|gals|qty|percent%]";

	public string Execute(string[] comps)
	{
		if (comps.Length < 3)
		{
			return "Usage: /setload <car|*> <identifier|empty> [lbs|gals|qty|percent%]";
		}
		if (StateManager.Shared.GameMode != GameMode.Sandbox)
		{
			return "Only available in Sandbox.";
		}
		string text = comps[1];
		string loadIdentifier = comps[2];
		if (text == "*")
		{
			List<Car> list = TrainController.Shared.SelectedTrain.Where((Car car2) => car2.Definition.Archetype.IsFreight()).ToList();
			foreach (Car item in list)
			{
				SetLoad(item, loadIdentifier, comps);
			}
			return "Applied to " + list.Count.Pluralize("freight car") + ".";
		}
		Car car = TrainController.Shared.CarForString(text);
		if (car != null)
		{
			return SetLoad(car, loadIdentifier, comps);
		}
		Industry industry = OpsController.Shared.IndustryForId(text);
		if (industry != null)
		{
			return SetLoad(industry, loadIdentifier, comps);
		}
		return "Query found no targets.";
	}

	private string SetLoad(Industry industry, string loadIdentifier, string[] comps)
	{
		Load load = TrainController.Shared.carPrototypeLibrary.LoadForId(loadIdentifier);
		if (load == null)
		{
			return "Load identifier not found: " + loadIdentifier;
		}
		float num = QuantityFromString(comps[3], 0f);
		industry.Storage.SetStorage(load, num);
		return $"Set load {load.description} to {num:F1}.";
	}

	private string SetLoad(Car car, string loadIdentifier, string[] comps)
	{
		CarPrototypeLibrary carPrototypeLibrary = TrainController.Shared.carPrototypeLibrary;
		int count = car.Definition.LoadSlots.Count;
		if (count == 0)
		{
			return "Car cannot accept loads.";
		}
		if (loadIdentifier == "empty" || loadIdentifier == "mt")
		{
			for (int i = 0; i < count; i++)
			{
				car.SetLoadInfo(i, null);
			}
			return "Car has been emptied.";
		}
		if (comps.Length < 4)
		{
			return "Usage: /setload <car|*> <identifier|empty> [lbs|gals|qty|percent%]";
		}
		string amountString = comps[3];
		Load load = carPrototypeLibrary.LoadForId(loadIdentifier);
		if (load == null)
		{
			return "Load identifier not found: " + loadIdentifier;
		}
		for (int j = 0; j < count; j++)
		{
			LoadSlot loadSlot = car.Definition.LoadSlots[j];
			CarLoadInfo? loadInfo = car.GetLoadInfo(j);
			if (loadSlot.RequiredLoadIdentifier == loadIdentifier || loadInfo?.LoadId == loadIdentifier)
			{
				return SetLoad(car, j, load, amountString);
			}
		}
		for (int k = 0; k < count; k++)
		{
			LoadSlot loadSlot2 = car.Definition.LoadSlots[k];
			car.GetLoadInfo(k);
			if (string.IsNullOrEmpty(loadSlot2.RequiredLoadIdentifier))
			{
				return SetLoad(car, k, load, amountString);
			}
		}
		return "Car doesn't have a slot that can accept " + load.description + ".";
	}

	private static string SetLoad(Car car, int slotIndex, Load load, string amountString)
	{
		LoadSlot loadSlot = car.Definition.LoadSlots[slotIndex];
		float quantity = QuantityFromString(amountString, loadSlot.MaximumCapacity);
		CarLoadInfo carLoadInfo = new CarLoadInfo(load.id, quantity);
		car.SetLoadInfo(slotIndex, carLoadInfo);
		return "Set load: " + carLoadInfo.LoadString(load);
	}

	private static float QuantityFromString(string amountString, float maximumCapacity)
	{
		if (amountString.EndsWith("%"))
		{
			return maximumCapacity * float.Parse(amountString.Substring(0, amountString.Length - 1)) / 100f;
		}
		return float.Parse(amountString);
	}

	public static void SetLoadPercent(Car car, string loadIdentifier, float percent)
	{
		CarPrototypeLibrary carPrototypeLibrary = TrainController.Shared.carPrototypeLibrary;
		int count = car.Definition.LoadSlots.Count;
		if (string.IsNullOrEmpty(loadIdentifier))
		{
			for (int i = 0; i < count; i++)
			{
				car.SetLoadInfo(i, null);
			}
			return;
		}
		if (count == 0)
		{
			throw new Exception("Car has no load slots");
		}
		Load load = carPrototypeLibrary.LoadForId(loadIdentifier);
		if (load == null)
		{
			throw new Exception("No such load");
		}
		for (int j = 0; j < count; j++)
		{
			LoadSlot loadSlot = car.Definition.LoadSlots[j];
			CarLoadInfo? loadInfo = car.GetLoadInfo(j);
			if (loadSlot.RequiredLoadIdentifier == loadIdentifier || loadInfo?.LoadId == loadIdentifier)
			{
				SetLoadHelper(j);
				return;
			}
		}
		for (int k = 0; k < count; k++)
		{
			if (string.IsNullOrEmpty(car.Definition.LoadSlots[k].RequiredLoadIdentifier))
			{
				SetLoadHelper(k);
				return;
			}
		}
		throw new Exception("Car doesn't have a slot that can accept " + load.description + ".");
		void SetLoadHelper(int slotIndex)
		{
			float quantity = car.Definition.LoadSlots[slotIndex].MaximumCapacity * percent;
			CarExtensions.SetLoadInfo(info: new CarLoadInfo(load.id, quantity), car: car, slot: slotIndex);
		}
	}
}
