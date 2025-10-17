using System;
using System.Collections.Generic;
using System.Linq;
using Model;
using Model.Ops;
using Serilog;
using Track;
using UnityEngine;

namespace UI.SwitchList;

public class OpsCarList
{
	public struct Entry
	{
		public struct Location
		{
			public readonly string Title;

			public readonly string Subtitle;

			public Vector3 Position;

			public int SortOrder;

			public readonly string[] SpanIds;

			public bool IsConcrete => SpanIds.Length != 0;

			public Location(string title, string subtitle, Vector3 position, int sortOrder, string[] spanIds)
			{
				Title = title;
				Subtitle = subtitle;
				Position = position;
				SortOrder = sortOrder;
				SpanIds = spanIds;
			}

			public Location(string title, string subtitle, Vector3 position, int sortOrder, IEnumerable<TrackSpan> spans)
				: this(title, subtitle, position, sortOrder, spans.Select((TrackSpan s) => s.id).ToArray())
			{
			}

			public bool Equals(Location other)
			{
				if (Title == other.Title && Subtitle == other.Subtitle && Vector3.Distance(Position, other.Position) < 1f)
				{
					return SpanIds.SequenceEqual(other.SpanIds);
				}
				return false;
			}

			public override bool Equals(object obj)
			{
				if (obj is Location other)
				{
					return Equals(other);
				}
				return false;
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(Title, Subtitle, Position, SpanIds);
			}
		}

		public readonly string CarId;

		public readonly string CarSortName;

		public Location Current;

		public Location Destination;

		public bool Completed;

		public Entry(string carId, string carSortName, Location current, Location destination, bool completed)
		{
			CarId = carId;
			CarSortName = carSortName;
			Current = current;
			Destination = destination;
			Completed = completed;
		}

		public bool Equals(Entry other)
		{
			if (CarId == other.CarId && Current.Equals(other.Current) && Destination.Equals(other.Destination))
			{
				return Completed == other.Completed;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is Entry other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(CarId, Current, Destination, Completed);
		}
	}

	public List<Entry> Entries = new List<Entry>();

	public bool Rebuild(IEnumerable<string> carIds)
	{
		TrainController trainController = TrainController.Shared;
		List<Entry> list = (from carId in carIds
			select MakeEntry(carId, trainController) into e
			where e.HasValue
			select e.Value).ToList();
		if (Entries == null || !list.SequenceEqual(Entries))
		{
			Entries = list;
			return true;
		}
		return false;
	}

	public bool Rebuild()
	{
		return Rebuild(Entries.Select((Entry e) => e.CarId));
	}

	public void SortByPositionDestination()
	{
		Entries.Sort(delegate(Entry a, Entry b)
		{
			int num = string.CompareOrdinal(a.Current.Title, b.Current.Title);
			return (num != 0) ? num : string.CompareOrdinal(a.Destination.Title, b.Destination.Title);
		});
	}

	private static Entry? MakeEntry(string carId, TrainController trainController)
	{
		Car car = trainController.CarForId(carId);
		if (car == null)
		{
			Log.Warning("Can't create entry for unknown car id {carId}", carId);
			return null;
		}
		OpsController shared = OpsController.Shared;
		if (shared == null)
		{
			return null;
		}
		Waybill? waybill = car.GetWaybill(shared);
		if (!waybill.HasValue)
		{
			Log.Error("Car {car} is in switch list but has no waybill.", car);
			return null;
		}
		Waybill value = waybill.Value;
		OpsCarPosition destination = value.Destination;
		Area area = shared.AreaForCarPosition(destination);
		string displayName = destination.DisplayName;
		Vector3 center = destination.GetCenter();
		Entry.Location destination2 = new Entry.Location(displayName, area?.name ?? "???", center, PositionSortOrder(center), destination.Spans);
		OpsCarPosition? opsCarPosition = shared.PositionForCar(car);
		Entry.Location current;
		if (!opsCarPosition.HasValue)
		{
			Area area2 = shared.ClosestArea(car);
			current = ((!(area2 != null)) ? new Entry.Location("???", null, Vector3.zero, 0, Array.Empty<string>()) : new Entry.Location(area2.name, null, Vector3.zero, 0, Array.Empty<string>()));
		}
		else
		{
			OpsCarPosition value2 = opsCarPosition.Value;
			string displayName2 = value2.DisplayName;
			Area area3 = shared.AreaForCarPosition(value2);
			Vector3 center2 = value2.GetCenter();
			current = new Entry.Location(displayName2, area3.name, center2, PositionSortOrder(center2), value2.Spans);
		}
		return new Entry(carId, car.SortName, current, destination2, value.Completed);
		static int PositionSortOrder(Vector3 vector)
		{
			return (int)vector.x / 10;
		}
	}
}
