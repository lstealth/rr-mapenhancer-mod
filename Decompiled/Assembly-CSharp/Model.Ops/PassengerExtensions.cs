using System;
using System.Collections.Generic;
using System.Linq;
using Game.State;
using Model.Definition.Data;

namespace Model.Ops;

public static class PassengerExtensions
{
	public static string PassengerCountString(this Car car, PassengerMarker? markerHint = null)
	{
		LoadSlot loadSlot = car.Definition.LoadSlots.FirstOrDefault((LoadSlot slot) => slot.RequiredLoadIdentifier == "passengers");
		return $"{CountPassengers()}/{(int)(loadSlot?.MaximumCapacity ?? 0f)}";
		int CountPassengers()
		{
			return (markerHint ?? car.GetPassengerMarker())?.TotalPassengers ?? 0;
		}
	}

	public static void SetPassengerDestinations(string carId, IEnumerable<string> destinations)
	{
		StateManager.AssertIsHost();
		if (!TrainController.Shared.TryGetCarForId(carId, out var car))
		{
			throw new ArgumentException("Car not found");
		}
		PassengerMarker value = car.GetPassengerMarker() ?? PassengerMarker.Empty();
		value.Destinations = destinations.ToHashSet();
		car.SetPassengerMarker(value);
	}

	public static void SetPassengerTimetableAutoDestinations(string carId, bool enable)
	{
		StateManager.AssertIsHost();
		if (!TrainController.Shared.TryGetCarForId(carId, out var car))
		{
			throw new ArgumentException("Car not found");
		}
		PassengerMarker value = car.GetPassengerMarker() ?? PassengerMarker.Empty();
		value.AutoDestinationsFromTimetable = enable;
		car.SetPassengerMarker(value);
	}
}
