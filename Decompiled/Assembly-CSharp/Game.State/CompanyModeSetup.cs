using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Messages;
using Game.Progression;
using KeyValue.Runtime;
using Model;
using Model.Definition;
using Model.Definition.Data;
using Model.Ops;
using Model.Ops.Definition;
using Serilog;
using Track;
using UnityEngine;

namespace Game.State;

public static class CompanyModeSetup
{
	public static IEnumerator Setup(TrainController trainController, SetupDescriptor setupDescriptor)
	{
		yield return new WaitForSeconds(5f);
		try
		{
			if (setupDescriptor.initialMoney > 0)
			{
				StateManager.Shared.ApplyToBalance(setupDescriptor.initialMoney, Ledger.Category.Bank, null, "Opening Balance");
			}
			SetupDescriptor.CarPlacement[] placements = setupDescriptor.placements;
			foreach (SetupDescriptor.CarPlacement carPlacement in placements)
			{
				Place(carPlacement.carIdentifier, carPlacement.marker.Location.Value, carPlacement.wreck, carPlacement.oiled, carPlacement.loadPercent, carPlacement.load);
			}
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			Log.Error(exception, "Error while adding starter equipment");
			Console.Log("Error adding starter equipment.");
		}
		void Place(IEnumerable<string> identifiers, Location location, bool wreckage, float oiled, float loadPercent, Load load)
		{
			List<CarDescriptor> list = StateManager.DescriptorsForIdentifiers(identifiers).Select(delegate(CarDescriptor d)
			{
				d.Properties["oiled"] = oiled;
				return d;
			}).ToList();
			if (wreckage)
			{
				string derailmentKey = PropertyChange.KeyForControl(PropertyChange.Control.Derailment);
				string conditionKey = PropertyChange.KeyForControl(PropertyChange.Control.Condition);
				list = list.Select(delegate(CarDescriptor d)
				{
					if (d.DefinitionInfo.Definition.Archetype == CarArchetype.Tender)
					{
						d.Properties["load.0"] = new CarLoadInfo("coal", 0f).AsPropertyValue;
						d.Properties["load.1"] = new CarLoadInfo("water", 0f).AsPropertyValue;
					}
					d.Properties[derailmentKey] = Value.Float(0.5f);
					d.Properties[conditionKey] = Value.Float(0.7f);
					return d;
				}).ToList();
			}
			if (load != null && loadPercent > 0f)
			{
				list = list.Select(delegate(CarDescriptor d)
				{
					if (d.DefinitionInfo.Definition.LoadSlots.Count == 0)
					{
						return d;
					}
					LoadSlot loadSlot = d.DefinitionInfo.Definition.LoadSlots[0];
					d.Properties["load.0"] = new CarLoadInfo(load.id, loadSlot.MaximumCapacity * loadPercent).AsPropertyValue;
					return d;
				}).ToList();
			}
			Log.Debug("Placing {descriptors} at {location}", list, location);
			trainController.PlaceTrain(location, list, null, loadPercent, PlaceTrainHandbrakes.None);
		}
	}
}
