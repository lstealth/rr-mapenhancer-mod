using System.Collections.Generic;
using System.Linq;
using Game;
using Game.Messages;
using Game.State;
using Model.Ops.Timetable;
using Serilog;
using UnityEngine;

namespace Model.Ops;

public static class PassengerStopTimetableLogic
{
	public struct GetTimetableDestinationsConfig
	{
		public int MinimumStopDuration;

		public AnimationCurve DepartureImmediacyToCoefficient;

		public AnimationCurve DepartureImmediacyGrowthChance;

		public AnimationCurve DepartureImmediacyToMultiplier;

		public AnimationCurve DeparturePastToCoefficient;
	}

	private struct Destination
	{
		public GameDateTime ArrivalTime;

		public int TransferCount;

		public string BestTrainName;
	}

	private const int StartWaitingHours = 1;

	private const int EndWaitingHours = 2;

	public static List<string> GetTimetableDestinations(Model.Ops.Timetable.Timetable timetable, GameDateTime now, string timetableCode, GetTimetableDestinationsConfig config, out float maxWaitingCoefficient, out float growthChance)
	{
		List<string> availableDestinations = new List<string>();
		HashSet<string> hashSet = new HashSet<string>();
		maxWaitingCoefficient = 0f;
		growthChance = 0f;
		foreach (KeyValuePair<string, Model.Ops.Timetable.Timetable.Train> train3 in timetable.Trains)
		{
			train3.Deconstruct(out var key, out var value);
			Model.Ops.Timetable.Timetable.Train train = value;
			if (train.TrainType != Model.Ops.Timetable.Timetable.TrainType.Passenger || !train.TryGetTimetableEntry(timetableCode, out var _, out var index) || index == train.Entries.Count - 1)
			{
				continue;
			}
			GameDateTime gameDateTime = train.GetGameDateTime(TimetableTimeType.Arrival, index, now);
			GameDateTime gameDateTime2 = train.GetGameDateTimeDeparture(index, now);
			if (now > gameDateTime2.AddingHours(12f))
			{
				gameDateTime = gameDateTime.AddingHours(24f);
				gameDateTime2 = gameDateTime2.AddingHours(24f);
			}
			if (gameDateTime == gameDateTime2)
			{
				gameDateTime = gameDateTime.AddingSeconds(-config.MinimumStopDuration);
			}
			GameDateTime gameDateTime3 = gameDateTime.AddingHours(-1f);
			GameDateTime gameDateTime4 = gameDateTime2.AddingHours(2f);
			if (gameDateTime3 < now && now < gameDateTime2)
			{
				float value2 = (float)(gameDateTime2 - now);
				float time = Mathf.InverseLerp(3600f, 0f, value2);
				maxWaitingCoefficient = Mathf.Max(maxWaitingCoefficient, config.DepartureImmediacyToCoefficient.Evaluate(time));
				growthChance = Mathf.Max(growthChance, config.DepartureImmediacyGrowthChance.Evaluate(time));
				int multiplier = Mathf.RoundToInt(config.DepartureImmediacyToMultiplier.Evaluate(time));
				GameDateTime gameDateTime5 = gameDateTime2.AddingHours(4f);
				HashSet<string> hashSet2 = train.Entries.Select((Model.Ops.Timetable.Timetable.Entry e) => e.Station).ToHashSet();
				for (int num = index + 1; num < train.Entries.Count; num++)
				{
					if (train.GetGameDateTime(TimetableTimeType.Arrival, num, now) > gameDateTime5)
					{
						break;
					}
					Model.Ops.Timetable.Timetable.Entry futureEntry = train.Entries[num];
					AddAvailableDestination(futureEntry.Station, multiplier);
					foreach (KeyValuePair<string, Model.Ops.Timetable.Timetable.Train> train4 in timetable.Trains)
					{
						train4.Deconstruct(out key, out value);
						Model.Ops.Timetable.Timetable.Train train2 = value;
						if (train2 == train || train2.TrainType != Model.Ops.Timetable.Timetable.TrainType.Passenger || train2.Entries.Any((Model.Ops.Timetable.Timetable.Entry e) => e.Station == timetableCode))
						{
							continue;
						}
						int num2 = train2.Entries.FindIndex((Model.Ops.Timetable.Timetable.Entry e) => e.Station == futureEntry.Station);
						if (num2 == -1 || !hashSet.Add(train2.Name) || train2.GetGameDateTimeDeparture(num2, now) < gameDateTime)
						{
							continue;
						}
						for (int num3 = num2 + 1; num3 < train2.Entries.Count; num3++)
						{
							Model.Ops.Timetable.Timetable.Entry entry = train2.Entries[num3];
							if (!hashSet2.Contains(entry.Station))
							{
								if (train2.GetGameDateTime(TimetableTimeType.Arrival, num3, now) > gameDateTime5)
								{
									break;
								}
								AddAvailableDestination(entry.Station, multiplier);
							}
						}
					}
				}
			}
			else if (gameDateTime2 <= now && now < gameDateTime4)
			{
				float time2 = Mathf.InverseLerp(0f, 7200f, (float)(now - gameDateTime2));
				maxWaitingCoefficient = Mathf.Max(maxWaitingCoefficient, config.DeparturePastToCoefficient.Evaluate(time2));
			}
		}
		return availableDestinations;
		void AddAvailableDestination(string stationCode, int num4)
		{
			for (int i = 0; i < num4; i++)
			{
				availableDestinations.Add(stationCode);
			}
		}
	}

	public static HashSet<string> GetDestinationsForTimetableTrain(Car car, Model.Ops.Timetable.Timetable timetable, string currentStationCode, GameDateTime now)
	{
		if (!car.TryGetTimetableTrain(out var train))
		{
			return null;
		}
		return GetDestinationsForTimetableTrain(timetable, train, currentStationCode, now);
	}

	public static HashSet<string> GetDestinationsForTimetableTrain(Model.Ops.Timetable.Timetable timetable, Model.Ops.Timetable.Timetable.Train train, string currentStationCode, GameDateTime now)
	{
		if (train.TrainType != Model.Ops.Timetable.Timetable.TrainType.Passenger)
		{
			return null;
		}
		int num = -1;
		for (int i = 0; i < train.Entries.Count; i++)
		{
			if (train.Entries[i].Station == currentStationCode)
			{
				num = i;
				break;
			}
			GameDateTime gameDateTime = train.GetGameDateTime(TimetableTimeType.Departure, i, now);
			if (now < gameDateTime)
			{
				num = i;
				break;
			}
		}
		if (num < 0)
		{
			return new HashSet<string>();
		}
		Dictionary<string, Destination> reachableDestinations = new Dictionary<string, Destination>();
		List<string> list = train.Entries.Select((Model.Ops.Timetable.Timetable.Entry e) => e.Station).Distinct().ToList();
		for (int num2 = num; num2 < train.Entries.Count; num2++)
		{
			Model.Ops.Timetable.Timetable.Entry entry = train.Entries[num2];
			GameDateTime gameDateTime2 = train.GetGameDateTime(TimetableTimeType.Arrival, num2, now);
			NoteDestination(entry.Station, gameDateTime2, 0, train.Name);
			if (entry.Station == currentStationCode)
			{
				continue;
			}
			foreach (var (_, train3) in timetable.Trains)
			{
				if (train3.Name == train.Name || train3.TrainType != Model.Ops.Timetable.Timetable.TrainType.Passenger)
				{
					continue;
				}
				int num3 = train3.Entries.FindIndex((Model.Ops.Timetable.Timetable.Entry e) => e.Station == entry.Station);
				if (num3 == -1 || train3.GetGameDateTimeDeparture(num3, now) <= gameDateTime2)
				{
					continue;
				}
				for (int num4 = num3 + 1; num4 < train3.Entries.Count; num4++)
				{
					Model.Ops.Timetable.Timetable.Entry entry2 = train3.Entries[num4];
					if (list.Contains(entry2.Station))
					{
						break;
					}
					GameDateTime gameDateTime3 = train3.GetGameDateTime(TimetableTimeType.Arrival, num4, now);
					if (!NoteDestination(entry2.Station, gameDateTime3, 1, train3.Name))
					{
						break;
					}
				}
			}
		}
		HashSet<string> hashSet = reachableDestinations.Keys.ToHashSet();
		if (currentStationCode != null)
		{
			hashSet.Remove(currentStationCode);
		}
		return hashSet;
		bool NoteDestination(string stationCode, GameDateTime arrivalTime, int transferCount, string trainName)
		{
			if (reachableDestinations.TryGetValue(stationCode, out var value))
			{
				if (arrivalTime > value.ArrivalTime)
				{
					return false;
				}
				value.TransferCount = transferCount;
				value.BestTrainName = trainName;
			}
			else
			{
				value = new Destination
				{
					ArrivalTime = arrivalTime,
					BestTrainName = trainName,
					TransferCount = transferCount
				};
			}
			reachableDestinations[stationCode] = value;
			return true;
		}
	}

	public static bool CopyStopsFromTimetable(this Car car, bool onlyIfAuto = false)
	{
		if (!car.TryGetTimetableTrain(out var train))
		{
			return false;
		}
		TimetableController shared = TimetableController.Shared;
		GameDateTime now = TimeWeather.Now;
		string currentStationCode = GetCurrentStationCode(car);
		PassengerMarker passengerMarker = car.GetPassengerMarker() ?? PassengerMarker.Empty();
		if (onlyIfAuto && !passengerMarker.AutoDestinationsFromTimetable)
		{
			return false;
		}
		HashSet<string> destinationsForTimetableTrain = GetDestinationsForTimetableTrain(shared.Current, train, currentStationCode, now);
		HashSet<string> hashSet = new HashSet<string>();
		foreach (string item in destinationsForTimetableTrain)
		{
			if (shared.TryGetPassengerStop(item, out var passengerStop))
			{
				hashSet.Add(passengerStop.identifier);
			}
		}
		if (hashSet.SequenceEqual(passengerMarker.Destinations))
		{
			return false;
		}
		Log.Information("CopyStopsFromTimetable {car} {train}, curr = {currentStation}, tt codes = {codes}: {old} -> {new}", car.DisplayName, train.Name, currentStationCode, destinationsForTimetableTrain, passengerMarker.Destinations, hashSet);
		StateManager.ApplyLocal(new SetPassengerDestinations(car.id, hashSet.ToList()));
		return true;
	}

	private static string GetCurrentStationCode(Car car)
	{
		TrainController trainController = TrainController.Shared;
		return (from ps in PassengerStop.FindAll()
			where !ps.ProgressionDisabled
			select ps).FirstOrDefault((PassengerStop ps) => ps.CarIsAtPassengerStop(car, trainController))?.timetableCode;
	}
}
