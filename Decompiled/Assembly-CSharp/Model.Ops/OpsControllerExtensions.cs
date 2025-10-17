using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using Serilog;
using UnityEngine;

namespace Model.Ops;

public static class OpsControllerExtensions
{
	public static bool TryGetDestinationInfo(this OpsController opsController, Car car, out string destinationName, out bool isAtDestination, out Vector3 destinationPosition, out OpsCarPosition destination)
	{
		destinationName = "";
		isAtDestination = false;
		destinationPosition = Vector3.zero;
		destination = default(OpsCarPosition);
		try
		{
			if (car.TryGetOverrideDestination(OverrideDestination.Repair, opsController, out (OpsCarPosition, string)? result))
			{
				destination = result.Value.Item1;
			}
			else
			{
				Waybill? waybill = car.GetWaybill(opsController);
				if (!waybill.HasValue)
				{
					return false;
				}
				destination = waybill.Value.Destination;
			}
			destinationName = opsController.NameForPosition(destination);
			destinationPosition = opsController.PointForPosition(destination);
			isAtDestination = opsController.CarsAtPosition(destination).Contains(car);
			return true;
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Error getting destination info for {car}", car);
			return false;
		}
		finally
		{
		}
	}

	public static void CycleAutoWaybill(this OpsController opsController, Car car, IEnumerable<Car> targets = null)
	{
		if (targets == null)
		{
			targets = new List<Car> { car };
		}
		OpsCarPosition? autoDestination = car.GetAutoDestination(AutoDestinationType.Empty, opsController);
		OpsCarPosition? autoDestination2 = car.GetAutoDestination(AutoDestinationType.Load, opsController);
		car.CycleAutoWaybill(opsController);
		Waybill? waybill = car.GetWaybill(opsController);
		if (!waybill.HasValue)
		{
			return;
		}
		foreach (Car target in targets)
		{
			if (!(target == car))
			{
				OpsCarPosition? autoDestination3 = target.GetAutoDestination(AutoDestinationType.Empty, opsController);
				OpsCarPosition? autoDestination4 = target.GetAutoDestination(AutoDestinationType.Load, opsController);
				if ((autoDestination3.HasValue || autoDestination4.HasValue) && (!autoDestination3.HasValue || autoDestination3.Equals(autoDestination)) && (!autoDestination4.HasValue || autoDestination4.Equals(autoDestination2)))
				{
					target.SetWaybill(waybill.Value);
				}
			}
		}
	}

	public static IndustryContext CreateContext(this IndustryComponent ic, GameDateTime now, float dt)
	{
		OpsController shared = OpsController.Shared;
		TrainController shared2 = TrainController.Shared;
		IndustryContext.CarSizePreference carSizePreference = shared2.CarSizePreference;
		Industry industry = ic.Industry;
		return new IndustryContext(shared2, shared, industry, ic, industry.KeyValueObject, carSizePreference, dt, now);
	}
}
