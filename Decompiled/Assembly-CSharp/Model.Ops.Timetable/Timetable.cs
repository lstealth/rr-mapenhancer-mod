using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;

namespace Model.Ops.Timetable;

public class Timetable
{
	public class Train
	{
		public string Name;

		public Direction Direction;

		public TrainClass TrainClass;

		public TrainType TrainType;

		public readonly List<Entry> Entries;

		public string SortName
		{
			get
			{
				if (!int.TryParse(Name, out var result))
				{
					return Name;
				}
				return $"{result:0000}";
			}
		}

		public int SortOrderWithinClass
		{
			get
			{
				if (Entries.Count == 0)
				{
					return int.MaxValue;
				}
				if (!TryGetAbsoluteTimeForEntry(0, TimetableTimeType.Departure, out var minutes))
				{
					return int.MaxValue;
				}
				return minutes;
			}
		}

		public string DisplayStringShort
		{
			get
			{
				string text = ((Direction == Direction.East) ? "E" : "W");
				return Name + " " + text;
			}
		}

		public string DisplayStringLong
		{
			get
			{
				string text = ((Direction == Direction.East) ? "E" : "W");
				string text2 = TrainClass switch
				{
					TrainClass.First => "1st", 
					TrainClass.Second => "2nd", 
					TrainClass.Third => "3rd", 
					_ => throw new ArgumentOutOfRangeException(), 
				};
				string text3 = ((TrainType == TrainType.Freight) ? "Freight" : "Pass.");
				return Name + " " + text + " - " + text3 + " " + text2;
			}
		}

		public Train(string name, Direction direction, TrainClass trainClass, TrainType trainType, List<Entry> entries)
		{
			Name = name;
			Direction = direction;
			TrainClass = trainClass;
			TrainType = trainType;
			Entries = entries;
		}

		public bool TryGetAbsoluteTimeForEntry(int index, TimetableTimeType type, out int minutes)
		{
			if (Entries.Count <= index || index < 0)
			{
				throw new IndexOutOfRangeException();
			}
			Entry entry = Entries[index];
			if (type == TimetableTimeType.Departure && entry.DepartureTime.IsAbsolute)
			{
				minutes = entry.DepartureTime.Minutes;
				return true;
			}
			if (type == TimetableTimeType.Arrival)
			{
				TimetableTime? arrivalTime = entry.ArrivalTime;
				if (arrivalTime.HasValue)
				{
					TimetableTime valueOrDefault = arrivalTime.GetValueOrDefault();
					if (valueOrDefault.IsAbsolute)
					{
						minutes = valueOrDefault.Minutes;
						return true;
					}
				}
			}
			bool flag = false;
			minutes = 0;
			for (int i = 0; i <= index; i++)
			{
				Entry entry2 = Entries[i];
				TimetableTime? arrivalTime = entry2.ArrivalTime;
				if (arrivalTime.HasValue)
				{
					TimetableTime valueOrDefault2 = arrivalTime.GetValueOrDefault();
					if (valueOrDefault2.IsAbsolute)
					{
						minutes = valueOrDefault2.Minutes;
						flag = true;
					}
					else if (flag)
					{
						minutes += valueOrDefault2.Minutes;
					}
					if (type == TimetableTimeType.Arrival && i == index)
					{
						break;
					}
				}
				if (entry2.DepartureTime.IsAbsolute)
				{
					flag = true;
					minutes = entry2.DepartureTime.Minutes;
				}
				else if (flag)
				{
					minutes += entry2.DepartureTime.Minutes;
				}
				if (type == TimetableTimeType.Arrival && i == index)
				{
					break;
				}
			}
			minutes %= 1440;
			return flag;
		}

		public Train Clone()
		{
			List<Entry> list = new List<Entry>(Entries.Count);
			foreach (Entry entry in Entries)
			{
				list.Add(new Entry(entry.Station, entry.ArrivalTime, entry.DepartureTime, entry.Meets));
			}
			return new Train(Name, Direction, TrainClass, TrainType, list);
		}

		public void AddEntry(Entry entry, IReadOnlyList<string> stationsEastToWest)
		{
			List<string> list = ((Direction == Direction.East) ? stationsEastToWest.Reverse().ToList() : stationsEastToWest.ToList());
			int num = list.IndexOf(entry.Station);
			if (num == -1)
			{
				throw new ArgumentException("Invalid station");
			}
			for (int i = 0; i < Entries.Count; i++)
			{
				if (list.IndexOf(Entries[i].Station) > num)
				{
					Entries.Insert(i, entry);
					return;
				}
			}
			Entries.Add(entry);
		}

		public void SortEntries(IReadOnlyList<string> stationsEastToWest)
		{
			Entries.Sort(delegate(Entry a, Entry b)
			{
				int num = Index(a.Station);
				int num2 = Index(b.Station);
				return (Direction != Direction.West) ? (num2 - num) : (num - num2);
			});
			int Index(string s)
			{
				for (int i = 0; i < stationsEastToWest.Count; i++)
				{
					if (stationsEastToWest[i] == s)
					{
						return i;
					}
				}
				return -1;
			}
		}

		protected bool Equals(Train other)
		{
			if (Name == other.Name && Direction == other.Direction && TrainClass == other.TrainClass && TrainType == other.TrainType)
			{
				return Entries.SequenceEqual(other.Entries);
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (this == obj)
			{
				return true;
			}
			if (obj.GetType() != GetType())
			{
				return false;
			}
			return Equals((Train)obj);
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}

		public bool StationsIntersectWithStationCodes(HashSet<string> stationCodes)
		{
			foreach (Entry entry in Entries)
			{
				foreach (string stationCode in stationCodes)
				{
					if (!(stationCode != entry.Station))
					{
						return true;
					}
				}
			}
			return false;
		}
	}

	public enum TrainClass
	{
		First,
		Second,
		Third
	}

	public enum TrainType
	{
		Freight,
		Passenger
	}

	public struct Entry : IEquatable<Entry>
	{
		public readonly string Station;

		public TimetableTime? ArrivalTime;

		public TimetableTime DepartureTime;

		public IReadOnlyList<string> Meets;

		public bool HasSingleArrivalAndDeparture
		{
			get
			{
				if (ArrivalTime.HasValue && (!ArrivalTime.Value.IsAbsolute || !ArrivalTime.Value.Equals(DepartureTime)))
				{
					if (!DepartureTime.IsAbsolute)
					{
						return DepartureTime.Minutes == 0;
					}
					return false;
				}
				return true;
			}
		}

		public Entry(string station, TimetableTime time, IReadOnlyList<string> meets)
		{
			Station = station;
			ArrivalTime = null;
			DepartureTime = time;
			Meets = meets;
		}

		public Entry(string station, TimetableTime? arrivalTime, TimetableTime departureTime, IReadOnlyList<string> meets)
		{
			Station = station;
			ArrivalTime = arrivalTime;
			DepartureTime = departureTime;
			Meets = meets;
		}

		public bool Equals(Entry other)
		{
			if (Station == other.Station && Nullable.Equals(ArrivalTime, other.ArrivalTime) && DepartureTime.Equals(other.DepartureTime))
			{
				return Meets.SequenceEqual(other.Meets);
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
			return HashCode.Combine(Station, ArrivalTime, DepartureTime, Meets);
		}
	}

	public enum Direction
	{
		West,
		East
	}

	public readonly Dictionary<string, Train> Trains;

	public Timetable()
	{
		Trains = new Dictionary<string, Train>();
	}

	public Timetable(Dictionary<string, Train> trains)
	{
		Trains = trains;
	}

	public Timetable ToAbsolute()
	{
		Dictionary<string, Train> dictionary = new Dictionary<string, Train>(Trains.Count);
		foreach (KeyValuePair<string, Train> train2 in Trains)
		{
			train2.Deconstruct(out var key, out var value);
			string key2 = key;
			Train train = value;
			List<Entry> list = new List<Entry>(train.Entries.Count);
			foreach (Entry entry in train.Entries)
			{
				int num;
				if (list.Count <= 0)
				{
					num = 0;
				}
				else
				{
					num = list[list.Count - 1].DepartureTime.Minutes;
				}
				int num2 = num;
				TimetableTime? arrivalTime = entry.ArrivalTime;
				TimetableTime? arrivalTime2;
				if (arrivalTime.HasValue)
				{
					TimetableTime valueOrDefault = arrivalTime.GetValueOrDefault();
					arrivalTime2 = ((!valueOrDefault.IsAbsolute) ? new TimetableTime?(TimetableTime.Absolute(num2 + valueOrDefault.Minutes)) : new TimetableTime?(valueOrDefault));
				}
				else
				{
					arrivalTime2 = null;
				}
				Entry item = new Entry(departureTime: (!entry.DepartureTime.IsAbsolute) ? ((!arrivalTime2.HasValue) ? TimetableTime.Absolute(num2 + entry.DepartureTime.Minutes) : TimetableTime.Absolute(arrivalTime2.GetValueOrDefault().Minutes + entry.DepartureTime.Minutes)) : entry.DepartureTime, station: entry.Station, arrivalTime: arrivalTime2, meets: entry.Meets);
				list.Add(item);
			}
			Train value2 = new Train(train.Name, train.Direction, train.TrainClass, train.TrainType, list);
			dictionary[key2] = value2;
		}
		return new Timetable(dictionary);
	}

	public Timetable Clone()
	{
		Dictionary<string, Train> dictionary = new Dictionary<string, Train>(Trains.Count);
		foreach (KeyValuePair<string, Train> train2 in Trains)
		{
			train2.Deconstruct(out var key, out var value);
			string key2 = key;
			Train train = value;
			dictionary[key2] = train.Clone();
		}
		return new Timetable(dictionary);
	}

	protected bool Equals(Timetable other)
	{
		return Trains.DictionaryEqual(other.Trains);
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (this == obj)
		{
			return true;
		}
		if (obj.GetType() != GetType())
		{
			return false;
		}
		return Equals((Timetable)obj);
	}

	public override int GetHashCode()
	{
		return Trains.GetHashCode();
	}
}
