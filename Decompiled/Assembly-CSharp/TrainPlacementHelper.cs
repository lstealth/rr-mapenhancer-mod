using System.Collections.Generic;
using System.Linq;
using Helpers;
using JetBrains.Annotations;
using Model;
using Model.Definition;
using Model.Ops;
using Model.Physics;
using Serilog;
using Track;
using UnityEngine;

public static class TrainPlacementHelper
{
	public static List<(Location location, List<CarDescriptor> descriptors)> FindTracksForCars(this TrainController trainController, List<CarDescriptor> descriptors, out Interchange chosenInterchange, OpsController opsController = null)
	{
		if ((object)opsController == null)
		{
			opsController = OpsController.Shared;
		}
		foreach (Interchange enabledInterchange in opsController.EnabledInterchanges)
		{
			List<TrackSpan> spans = enabledInterchange.TrackSpans.ToList();
			List<(Location, List<(CarDescriptor, string)>)> list = trainController.FindLocationsForCars(spans, descriptors, null);
			if (list == null)
			{
				continue;
			}
			chosenInterchange = enabledInterchange;
			return list.Select<(Location, List<(CarDescriptor, string)>), (Location, List<CarDescriptor>)>(((Location location, List<(CarDescriptor descriptor, string carId)> descriptorsAndIds) tuple) => (location: tuple.location, tuple.descriptorsAndIds.Select(((CarDescriptor descriptor, string carId) t) => t.descriptor).ToList())).ToList();
		}
		chosenInterchange = null;
		return null;
	}

	public static bool PlaceTrain(this TrainController trainController, List<TrackSpan> spans, List<CarDescriptor> descriptors, List<string> carIds)
	{
		Log.Information("PlaceTrain {spans}, {carCount} cars", spans, descriptors.Count);
		List<(Location, List<(CarDescriptor, string)>)> list = trainController.FindLocationsForCars(spans, descriptors, carIds);
		if (list == null)
		{
			return false;
		}
		foreach (var item3 in list)
		{
			Location item = item3.Item1;
			List<(CarDescriptor, string)> item2 = item3.Item2;
			List<CarDescriptor> descriptors2 = item2.Select<(CarDescriptor, string), CarDescriptor>(((CarDescriptor descriptor, string carId) tuple) => tuple.descriptor).ToList();
			List<string> carIds2 = item2.Select<(CarDescriptor, string), string>(((CarDescriptor descriptor, string carId) tuple) => tuple.carId).ToList();
			trainController.PlaceTrain(item, descriptors2, carIds2);
		}
		return true;
	}

	[CanBeNull]
	private static List<(Location location, List<(CarDescriptor descriptor, string carId)> descriptorsAndIds)> FindLocationsForCars(this TrainController trainController, List<TrackSpan> spans, List<CarDescriptor> descriptors, List<string> carIds)
	{
		Log.Information("PlaceTrain {spans}, {carCount} cars", spans, descriptors.Count);
		List<(Location, int)> list = new List<(Location, int)>();
		List<CarDescriptor> list2 = new List<CarDescriptor>(descriptors);
		List<TrackSpan> list3 = spans.ToList();
		while (list2.Any())
		{
			if (list3.Count == 0)
			{
				return null;
			}
			Location locationOut;
			TrackSpan chosenSpan;
			int num = trainController.FindLargestPlaceableCut(list3, list2, out locationOut, out chosenSpan);
			if (num == 0)
			{
				return null;
			}
			list.Add((locationOut, num));
			list2.RemoveRange(0, num);
			list3.Remove(chosenSpan);
		}
		List<(Location, List<(CarDescriptor, string)>)> list4 = new List<(Location, List<(CarDescriptor, string)>)>();
		int num2 = 0;
		foreach (var item3 in list)
		{
			Location item = item3.Item1;
			int item2 = item3.Item2;
			List<(CarDescriptor, string)> list5 = new List<(CarDescriptor, string)>();
			for (int i = num2; i < num2 + item2; i++)
			{
				list5.Add((descriptors[i], carIds?[i]));
			}
			list4.Add((item, list5));
			num2 += item2;
		}
		return list4;
	}

	private static int FindLargestPlaceableCut(this TrainController trainController, List<TrackSpan> spans, List<CarDescriptor> remaining, out Location locationOut, out TrackSpan chosenSpan)
	{
		int num = 1;
		int num2 = remaining.Count;
		Location? location = null;
		int result = 0;
		chosenSpan = null;
		while (num <= num2)
		{
			int num3 = (num + num2) / 2;
			if (!CanBreakAtIndex(remaining, num3))
			{
				if (num3 + 1 > num2)
				{
					num3--;
					if (num3 < num)
					{
						break;
					}
				}
				else
				{
					num3++;
				}
			}
			float requiredLength = TrainController.ApproximateLength(remaining.Take(num3).ToList());
			TrackSpan chosenSpan2;
			Location? location2 = trainController.FindLocationForCutFromEnd(spans, requiredLength, out chosenSpan2);
			if (location2.HasValue)
			{
				location = location2;
				chosenSpan = chosenSpan2;
				result = num3;
				num = num3 + 1;
			}
			else
			{
				num2 = num3 - 1;
			}
		}
		locationOut = location.GetValueOrDefault();
		return result;
	}

	private static bool CanBreakAtIndex(List<CarDescriptor> descriptors, int index)
	{
		if (index == 0 || index == descriptors.Count)
		{
			return true;
		}
		CarDescriptor carDescriptor = descriptors[index - 1];
		CarDescriptor carDescriptor2 = descriptors[index];
		CarArchetype archetype = carDescriptor.DefinitionInfo.Definition.Archetype;
		CarArchetype archetype2 = carDescriptor2.DefinitionInfo.Definition.Archetype;
		if (archetype == CarArchetype.Tender && carDescriptor.Flipped && archetype2 == CarArchetype.LocomotiveSteam && carDescriptor2.Flipped)
		{
			return false;
		}
		if (archetype2 == CarArchetype.Tender && !carDescriptor2.Flipped && archetype == CarArchetype.LocomotiveSteam && !carDescriptor.Flipped)
		{
			return false;
		}
		return true;
	}

	private static Location? FindLocationForCutFromEnd(this TrainController trainController, List<TrackSpan> spans, float requiredLength, out TrackSpan chosenSpan, float buffer = 10f)
	{
		Location? result;
		foreach (TrackSpan span in spans)
		{
			TrackSpan trackSpan = (chosenSpan = span);
			List<Car> list = trainController.CarsOnSpan(trackSpan).ToList();
			result = trackSpan.lower;
			Location value = result.Value;
			if (list.Count == 0 && trackSpan.Length > requiredLength)
			{
				Location location = value.Flipped();
				if (ConfirmCanPlaceAt(location))
				{
					result = location;
					goto IL_0141;
				}
			}
			Location? location2 = FindFirstCar(trainController, value, requiredLength, trackSpan, 2f, buffer);
			if (location2.HasValue)
			{
				Location valueOrDefault = location2.GetValueOrDefault();
				if (ConfirmCanPlaceAt(valueOrDefault))
				{
					result = valueOrDefault;
					goto IL_0141;
				}
			}
			value = trackSpan.upper.Value;
			location2 = FindFirstCar(trainController, value, requiredLength, trackSpan, 2f, buffer);
			if (!location2.HasValue)
			{
				continue;
			}
			Location valueOrDefault2 = location2.GetValueOrDefault();
			if (!ConfirmCanPlaceAt(valueOrDefault2))
			{
				continue;
			}
			result = valueOrDefault2;
			goto IL_0141;
		}
		chosenSpan = null;
		return null;
		IL_0141:
		return result;
		bool ConfirmCanPlaceAt(Location loc)
		{
			return trainController.CanPlaceAt(loc, requiredLength);
		}
	}

	private static Location? FindFirstCar(TrainController trainController, Location start, float requiredLength, TrackSpan trackSpan, float step, float buffer)
	{
		Location location = start;
		float num = 0f;
		while (true)
		{
			try
			{
				Location location2 = trainController.graph.LocationByMoving(location, step, checkSwitchAgainstMovement: false, stopAtEndOfTrack: true);
				if (location2.Equals(location))
				{
					return null;
				}
				bool flag = trainController.CheckForCarAtLocation(location2) != null || !trackSpan.Contains(location2);
				Debug.DrawRay(WorldTransformer.GameToWorld(location2.GetPosition()), Vector3.up, flag ? Color.red : Color.green, 1f);
				if (flag)
				{
					break;
				}
				location = location2;
				num += step;
				continue;
			}
			catch (EndOfTrack)
			{
				return null;
			}
		}
		if (num < requiredLength + buffer * 2f)
		{
			return null;
		}
		return trainController.graph.LocationByMoving(location, 0f - buffer, checkSwitchAgainstMovement: false, stopAtEndOfTrack: true);
	}

	public static Location? FindLocationForCut(this TrainController trainController, List<TrackSegment> segments, float requiredLength, float buffer = 10f)
	{
		Location? result = null;
		foreach (var item3 in Graph.EnumerateContinuousSegments(segments))
		{
			List<TrackSegment> item = item3.Item1;
			List<TrackNode> item2 = item3.Item2;
			List<Car> list = trainController.CarsOnSegments(item).ToList();
			if (list.Exists((Car c) => !c.Archetype.IsFreight()))
			{
				continue;
			}
			if (list.Count == 0 && item.Sum((TrackSegment segment) => segment.GetLength()) > requiredLength)
			{
				result = new Location(item[0], 0f, item[0].EndForNode(item2[0])).Flipped();
				break;
			}
			List<(Car.LogicalEnd, Car)> list2 = new List<(Car.LogicalEnd, Car)>();
			foreach (Car item4 in list)
			{
				if (item4.set == null)
				{
					list2.Add((Car.LogicalEnd.A, item4));
					list2.Add((Car.LogicalEnd.B, item4));
					continue;
				}
				switch (item4.set.PositionOfCar(item4))
				{
				case IntegrationSet.PositionInSet.A:
					list2.Add((Car.LogicalEnd.A, item4));
					break;
				case IntegrationSet.PositionInSet.B:
					list2.Add((Car.LogicalEnd.B, item4));
					break;
				case IntegrationSet.PositionInSet.Solo:
					list2.Add((Car.LogicalEnd.A, item4));
					list2.Add((Car.LogicalEnd.B, item4));
					break;
				}
			}
			while (list2.Count > 0)
			{
				var (logical, car) = list2[0];
				list2.RemoveAt(0);
				Location start = ((car.LogicalToEnd(logical) == Car.End.F) ? car.LocationF : car.LocationR.Flipped());
				try
				{
					start = trainController.graph.LocationByMoving(start, buffer);
					Location location = start;
					float num = requiredLength + buffer;
					Car car2 = null;
					while (car2 == null && num > 0f && item.Contains(start.segment))
					{
						num -= 2f;
						start = trainController.graph.LocationByMoving(start, 2f);
						car2 = trainController.CheckForCarAtLocation(start);
					}
					if (car2 == null && num <= 0f)
					{
						result = location.Flipped();
						break;
					}
				}
				catch (EndOfTrack)
				{
				}
			}
		}
		return result;
	}
}
