using System.Collections.Generic;
using System.Linq;
using Game;
using KeyValue.Runtime;

namespace Model.Ops;

public struct PassengerMarker
{
	public readonly List<PassengerGroup> Groups;

	public HashSet<string> Destinations;

	public string LastStopIdentifier;

	public bool AutoDestinationsFromTimetable;

	public int TotalPassengers => Groups.Sum((PassengerGroup group) => group.Count);

	public PassengerMarker(List<PassengerGroup> groups, HashSet<string> destinations, string lastStopIdentifier, bool autoDestinationsFromTimetable)
	{
		Groups = groups;
		Destinations = destinations;
		LastStopIdentifier = lastStopIdentifier;
		AutoDestinationsFromTimetable = autoDestinationsFromTimetable;
	}

	public static PassengerMarker Empty()
	{
		return new PassengerMarker(new List<PassengerGroup>(), new HashSet<string>(), null, autoDestinationsFromTimetable: false);
	}

	public override string ToString()
	{
		return string.Join(", ", Groups.Select((PassengerGroup group) => group.ToString()));
	}

	public static PassengerMarker? FromPropertyValue(Value value)
	{
		if (value.Type != ValueType.Dictionary)
		{
			return null;
		}
		IReadOnlyDictionary<string, Value> dictionaryValue = value.DictionaryValue;
		if (!dictionaryValue.TryGetValue("groups", out var value2) || !dictionaryValue.TryGetValue("destinations", out var value3))
		{
			return null;
		}
		if (value2.Type != ValueType.Array)
		{
			return null;
		}
		List<PassengerGroup> groups = value2.ArrayValue.Select(PassengerGroup.FromPropertyValue).ToList();
		HashSet<string> destinations = value3.ArrayValue.Select((Value v) => v.StringValue).ToHashSet();
		bool boolValue = value["ttAutoDest"].BoolValue;
		string lastStopIdentifier = null;
		if (dictionaryValue.TryGetValue("lastStop", out var value4))
		{
			lastStopIdentifier = (value4.IsNull ? null : value4.StringValue);
		}
		return new PassengerMarker(groups, destinations, lastStopIdentifier, boolValue);
	}

	public Value PropertyValue()
	{
		return Value.Dictionary(new Dictionary<string, Value>
		{
			{
				"groups",
				Value.Array(Groups.Select((PassengerGroup g) => g.PropertyValue()).ToList())
			},
			{
				"destinations",
				Value.Array(Destinations.Select(Value.String).ToList())
			},
			{
				"lastStop",
				string.IsNullOrEmpty(LastStopIdentifier) ? Value.Null() : Value.String(LastStopIdentifier)
			},
			{
				"ttAutoDest",
				AutoDestinationsFromTimetable ? ((Value)true) : Value.Null()
			}
		});
	}

	public int CountPassengersForStop(string stopIdentifier)
	{
		return Groups.Where((PassengerGroup group) => group.Destination == stopIdentifier).Sum((PassengerGroup group) => group.Count);
	}

	public void AddPassengers(string origin, string destination, int num, GameDateTime boarded)
	{
		for (int i = 0; i < Groups.Count; i++)
		{
			PassengerGroup value = Groups[i];
			if (!(value.Destination != destination) && !(value.Origin != origin) && !(boarded.TotalSeconds - value.Boarded.TotalSeconds > 600.0))
			{
				value.Count += num;
				Groups[i] = value;
				return;
			}
		}
		Groups.Add(new PassengerGroup(origin, destination, num, boarded));
	}

	public bool TryRemovePassenger(string destination, out string removedDestination, out string removedOrigin, out GameDateTime removedBoarded)
	{
		for (int i = 0; i < Groups.Count; i++)
		{
			PassengerGroup value = Groups[i];
			if (value.Count <= 0)
			{
				continue;
			}
			bool flag = Destinations.Contains(value.Destination);
			if (!(!(value.Destination == destination) && flag))
			{
				value.Count--;
				if (value.Count > 0)
				{
					Groups[i] = value;
				}
				else
				{
					Groups.RemoveAt(i);
					i--;
				}
				removedOrigin = value.Origin;
				removedBoarded = value.Boarded;
				removedDestination = value.Destination;
				return true;
			}
		}
		removedDestination = null;
		removedBoarded = default(GameDateTime);
		removedOrigin = null;
		return false;
	}
}
