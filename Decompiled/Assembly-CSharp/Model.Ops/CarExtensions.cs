using System;
using System.Collections.Generic;
using System.Globalization;
using Game;
using Game.State;
using KeyValue.Runtime;
using Model.Definition;
using Model.Definition.Data;
using Model.Ops.Definition;
using Model.Ops.Timetable;
using Serilog;
using UnityEngine;

namespace Model.Ops;

public static class CarExtensions
{
	public const string TagAutodest = "autodest";

	public const string TagSell = "sell";

	public static CarLoadInfo? GetLoadInfo(this Car car, int slot)
	{
		return CarLoadInfo.FromPropertyValue(car.KeyValueObject[KeyForLoadInfoSlot(slot)]);
	}

	public static void SetLoadInfo(this Car car, int slot, CarLoadInfo? info)
	{
		var (key, value) = KeyValueForLoadInfo(slot, info);
		car.KeyValueObject[key] = value;
	}

	public static CarLoadInfo? GetLoadInfo(this Car car, string loadIdentifier, out int slotIndex)
	{
		for (slotIndex = 0; slotIndex < car.Definition.LoadSlots.Count; slotIndex++)
		{
			LoadSlot loadSlot = car.Definition.LoadSlots[slotIndex];
			if (!string.IsNullOrEmpty(loadSlot.RequiredLoadIdentifier) && loadSlot.RequiredLoadIdentifier == loadIdentifier)
			{
				return car.GetLoadInfo(slotIndex);
			}
		}
		for (slotIndex = 0; slotIndex < car.Definition.LoadSlots.Count; slotIndex++)
		{
			CarLoadInfo? loadInfo = car.GetLoadInfo(slotIndex);
			if (loadInfo.HasValue && loadInfo.Value.LoadId == loadIdentifier)
			{
				return loadInfo.Value;
			}
		}
		slotIndex = -1;
		return null;
	}

	public static bool IsLoadEmpty(this Car car)
	{
		for (int i = 0; i < car.Definition.LoadSlots.Count; i++)
		{
			CarLoadInfo? loadInfo = car.GetLoadInfo(i);
			if (loadInfo.HasValue && loadInfo.Value.Quantity > 0.001f)
			{
				return false;
			}
		}
		return true;
	}

	public static (float quantity, float capacity) QuantityCapacityOfLoad(this Car car, Load load)
	{
		List<LoadSlot> loadSlots = car.Definition.LoadSlots;
		for (int i = 0; i < loadSlots.Count; i++)
		{
			CarLoadInfo? loadInfo = car.GetLoadInfo(i);
			if (loadInfo.HasValue && loadInfo.Value.LoadId == load.id)
			{
				return (quantity: loadInfo.Value.Quantity, capacity: loadSlots[i].MaximumCapacity);
			}
		}
		foreach (LoadSlot item in loadSlots)
		{
			if (item.LoadRequirementsMatch(load))
			{
				return (quantity: 0f, capacity: item.MaximumCapacity);
			}
		}
		return (quantity: 0f, capacity: load.NominalQuantityPerCarLoad);
	}

	public static string LoadString(this CarLoadInfo info, Load load)
	{
		string text;
		switch (load.units)
		{
		case LoadUnits.Pounds:
		{
			float quantity = info.Quantity;
			string text2 = ((quantity < 1f) ? $"{info.Quantity:N1}lb" : ((!(quantity < 200f)) ? $"{info.Quantity / 2000f:N1}T" : $"{info.Quantity:N0}lb"));
			text = text2;
			break;
		}
		case LoadUnits.Gallons:
			text = $"{Mathf.RoundToInt(info.Quantity):N0} gal";
			break;
		case LoadUnits.Quantity:
			text = ((float)Math.Round(info.Quantity, 1)).ToString(CultureInfo.CurrentCulture);
			break;
		default:
			text = info.Quantity.ToString();
			break;
		}
		return text + " " + load.description;
	}

	public static (string, Value) KeyValueForLoadInfo(int slot, CarLoadInfo? info)
	{
		Value item = info?.AsPropertyValue ?? Value.Null();
		return (KeyForLoadInfoSlot(slot), item);
	}

	public static string KeyForLoadInfoSlot(int slot)
	{
		return $"load.{slot}";
	}

	public static Waybill? GetWaybill(this Car car, IOpsCarPositionResolver resolver)
	{
		return car.Waybill;
	}

	public static void CheckWaybill(this Car car, IOpsCarPositionResolver resolver)
	{
		Value value = car.KeyValueObject["ops.waybill"];
		try
		{
			Waybill.FromPropertyValue(value, resolver);
		}
		catch (Exception innerException)
		{
			throw new Exception($"{car} has malformed waybill", innerException);
		}
	}

	public static void SetWaybill(this Car car, Waybill? waybill)
	{
		car.KeyValueObject["ops.waybill"] = waybill?.PropertyValue ?? Value.Null();
	}

	public static void ApplyAutoWaybillIfNeeded(this Car car, IOpsCarPositionResolver resolver)
	{
		if (!car.GetWaybill(resolver).HasValue)
		{
			car.SetWaybillAuto(null, resolver);
		}
	}

	public static void SetWaybillAuto(this Car car, Waybill? waybill, IOpsCarPositionResolver resolver)
	{
		if (!waybill.HasValue)
		{
			AutoDestinationType autoDestinationType = (car.IsLoadEmpty() ? AutoDestinationType.Empty : AutoDestinationType.Load);
			OpsCarPosition? autoDestination = car.GetAutoDestination(autoDestinationType, resolver);
			if (autoDestination.HasValue)
			{
				waybill = new Waybill(TimeWeather.Now, null, autoDestination.Value, 0, completed: false, "autodest", 0);
				Log.Information("SetWaybillAuto: {car} using auto destination {autoDestinationType} {waybill}", car, autoDestinationType, waybill);
			}
		}
		car.SetWaybill(waybill);
	}

	public static void CycleAutoWaybill(this Car car, IOpsCarPositionResolver resolver)
	{
		Waybill? existing = car.GetWaybill(resolver);
		OpsCarPosition? autoDestination = car.GetAutoDestination(AutoDestinationType.Empty, resolver);
		OpsCarPosition? autoDestination2 = car.GetAutoDestination(AutoDestinationType.Load, resolver);
		OpsCarPosition? opsCarPosition = ((autoDestination.HasValue && autoDestination2.HasValue) ? new OpsCarPosition?(ExistingEquals(autoDestination.Value) ? autoDestination2.Value : autoDestination.Value) : (autoDestination.HasValue ? ((!ExistingEquals(autoDestination.Value)) ? new OpsCarPosition?(autoDestination.Value) : ((OpsCarPosition?)null)) : ((!autoDestination2.HasValue) ? ((OpsCarPosition?)null) : ((!ExistingEquals(autoDestination2.Value)) ? new OpsCarPosition?(autoDestination2.Value) : ((OpsCarPosition?)null)))));
		Waybill? waybill = ((!opsCarPosition.HasValue) ? ((Waybill?)null) : new Waybill?(new Waybill(TimeWeather.Now, null, opsCarPosition.Value, 0, completed: false, "autodest", 0)));
		car.SetWaybill(waybill);
		bool ExistingEquals(OpsCarPosition pos)
		{
			if (existing.HasValue)
			{
				return existing.Value.Destination.Equals(pos);
			}
			return false;
		}
	}

	private static string KeyAutoDestination(AutoDestinationType destinationType)
	{
		return destinationType switch
		{
			AutoDestinationType.Load => "ops.autodest.ld", 
			AutoDestinationType.Empty => "ops.autodest.mt", 
			_ => throw new ArgumentOutOfRangeException("destinationType", destinationType, null), 
		};
	}

	public static OpsCarPosition? GetAutoDestination(this Car car, AutoDestinationType destinationType, IOpsCarPositionResolver resolver)
	{
		string key = KeyAutoDestination(destinationType);
		Value value = car.KeyValueObject[key];
		if (value.IsNull)
		{
			return null;
		}
		try
		{
			return resolver.ResolveOpsCarPosition(value.StringValue);
		}
		catch
		{
			Log.Warning("Car {car} has bad auto destination {value}", car, value.StringValue);
			return null;
		}
	}

	public static bool SetAutoDestination(this Car car, AutoDestinationType destinationType, OpsCarPosition? destination)
	{
		if (destination.HasValue && !OpsController.Shared.CanWaybillTo(car, destination.Value))
		{
			Log.Warning("Ignoring SetAutoDestination: {car} cannot be waybilled to {destination}", car, destination.Value);
			return false;
		}
		string key = KeyAutoDestination(destinationType);
		car.KeyValueObject[key] = ((!destination.HasValue) ? Value.Null() : Value.String(destination.Value.Identifier));
		return true;
	}

	public static PassengerMarker? GetPassengerMarker(this Car car)
	{
		return PassengerMarker.FromPropertyValue(car.KeyValueObject["ops.passengerMarker"]);
	}

	public static void SetPassengerMarker(this Car car, PassengerMarker? value)
	{
		StateManager.AssertIsHost();
		Log.Debug("Saving passenger marker to {car}: {marker}", car, value);
		car.KeyValueObject["ops.passengerMarker"] = value?.PropertyValue() ?? Value.Null();
	}

	public static bool IsPassengerCar(this Car car)
	{
		return car.Definition.IsPassengerCar();
	}

	public static bool IsPassengerCar(this CarDefinition carDefinition)
	{
		if (carDefinition.Archetype switch
		{
			CarArchetype.LocomotiveDiesel => 1, 
			CarArchetype.LocomotiveSteam => 1, 
			CarArchetype.Caboose => 1, 
			CarArchetype.Coach => 1, 
			CarArchetype.Baggage => 1, 
			_ => 0, 
		} == 0)
		{
			return false;
		}
		foreach (LoadSlot loadSlot in carDefinition.LoadSlots)
		{
			if (loadSlot.RequiredLoadIdentifier == "passengers")
			{
				return true;
			}
		}
		return false;
	}

	public static bool TryGetTimetableTrainCrewId(this Car car, out string trainCrewId)
	{
		trainCrewId = car.trainCrewId;
		if (!string.IsNullOrEmpty(trainCrewId))
		{
			return true;
		}
		foreach (Car item in car.EnumerateCoupled())
		{
			if (!string.IsNullOrEmpty(item.trainCrewId))
			{
				trainCrewId = item.trainCrewId;
				break;
			}
		}
		return !string.IsNullOrEmpty(trainCrewId);
	}

	public static bool TryGetTimetableTrain(this Car car, out Model.Ops.Timetable.Timetable.Train train)
	{
		train = null;
		if (!car.TryGetTimetableTrainCrewId(out var trainCrewId))
		{
			return false;
		}
		TimetableController shared = TimetableController.Shared;
		if (shared == null)
		{
			return false;
		}
		return shared.TryGetTrainForTrainCrewId(trainCrewId, out train);
	}

	public static bool TryGetTrainName(this Car car, out string trainName)
	{
		trainName = null;
		if (string.IsNullOrEmpty(car.trainCrewId))
		{
			return false;
		}
		if (!StateManager.Shared.PlayersManager.TrainCrewForId(car.trainCrewId, out var trainCrew))
		{
			return false;
		}
		if (TimetableController.Shared.TryGetTrainForTrainCrew(trainCrew, out var timetableTrain))
		{
			trainName = "Train " + timetableTrain.DisplayStringShort;
		}
		else
		{
			trainName = trainCrew.Name;
		}
		return true;
	}
}
