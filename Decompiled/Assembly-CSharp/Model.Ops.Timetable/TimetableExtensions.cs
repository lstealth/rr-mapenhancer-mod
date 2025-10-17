using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using UnityEngine;

namespace Model.Ops.Timetable;

public static class TimetableExtensions
{
	public static bool TryGetTimetableEntry(this Timetable.Train train, string shortName, out Timetable.Entry stationEntry, out int index)
	{
		for (int i = 0; i < train.Entries.Count; i++)
		{
			Timetable.Entry entry = train.Entries[i];
			if (!(entry.Station != shortName))
			{
				stationEntry = entry;
				index = i;
				return true;
			}
		}
		stationEntry = default(Timetable.Entry);
		index = -1;
		return false;
	}

	public static GameDateTime GameDateTimeFromTimetable(int timetableTime, GameDateTime now)
	{
		return now.StartOfDay.AddingMinutes(timetableTime);
	}

	public static GameDateTime GetGameDateTimeDeparture(this Timetable.Train train, int index, GameDateTime now)
	{
		return train.GetGameDateTime(TimetableTimeType.Departure, index, now);
	}

	public static GameDateTime GetGameDateTime(this Timetable.Train train, TimetableTimeType timeType, int index, GameDateTime now)
	{
		if (!train.TryGetAbsoluteTimeForEntry(index, timeType, out var minutes))
		{
			throw new Exception("No absolute time before entry");
		}
		GameDateTime gameDateTime = now.StartOfDay.AddingMinutes(minutes);
		if (timeType == TimetableTimeType.Departure && train.TryGetAbsoluteTimeForEntry(index, TimetableTimeType.Arrival, out var minutes2) && minutes2 > minutes)
		{
			GameDateTime gameDateTime2 = gameDateTime;
			GameDateTime result = gameDateTime2.AddingDays(1f);
			GameDateTime gameDateTime3 = train.GetGameDateTime(TimetableTimeType.Arrival, index, now);
			if (gameDateTime3 < now)
			{
				return result;
			}
			if (now < gameDateTime2)
			{
				return gameDateTime2;
			}
			float num = Mathf.Abs((float)(now - gameDateTime2));
			if (!(Mathf.Abs((float)(now - gameDateTime3)) < num))
			{
				return gameDateTime2;
			}
			return result;
		}
		return RollTimeToTomorrowIfTooOld(gameDateTime, now);
	}

	private static GameDateTime RollTimeToTomorrowIfTooOld(GameDateTime timeToday, GameDateTime now)
	{
		GameDateTime gameDateTime = now.AddingHours(-12f);
		if (!(timeToday < gameDateTime))
		{
			return timeToday;
		}
		return timeToday.AddingDays(1f);
	}

	public static HashSet<string> GetIllogicalStations(this Timetable.Train train, List<TimetableBranch> branches)
	{
		HashSet<string> hashSet = new HashSet<string>();
		if (branches.Count == 0)
		{
			return hashSet;
		}
		HashSet<string> selectedStations = train.Entries.Select((Timetable.Entry e) => e.Station).ToHashSet();
		TimetableBranch timetableBranch = branches[0];
		Dictionary<string, TimetableBranch> dictionary = new Dictionary<string, TimetableBranch>();
		for (int num = 1; num < branches.Count; num++)
		{
			TimetableBranch timetableBranch2 = branches[num];
			foreach (TimetableStation station in timetableBranch2.stations)
			{
				if (station.IsBranchJunctionDuplicate)
				{
					dictionary.Add(station.code, timetableBranch2);
					break;
				}
			}
			if (!timetableBranch2.stations.Any((TimetableStation station) => !station.IsBranchJunctionDuplicate && selectedStations.Contains(station.code)))
			{
				continue;
			}
			Timetable.Direction junctionEnd;
			int indexOfBranchJunctionOnMain = GetIndexOfBranchJunctionOnMain(timetableBranch, timetableBranch2, out junctionEnd);
			if (train.Direction == Timetable.Direction.West && junctionEnd == Timetable.Direction.West)
			{
				for (int num2 = 0; num2 < indexOfBranchJunctionOnMain; num2++)
				{
					hashSet.Add(timetableBranch.stations[num2].code);
				}
			}
			else if (train.Direction == Timetable.Direction.East && junctionEnd == Timetable.Direction.East)
			{
				for (int num3 = indexOfBranchJunctionOnMain + 1; num3 < timetableBranch.stations.Count; num3++)
				{
					hashSet.Add(timetableBranch.stations[num3].code);
				}
			}
		}
		for (int num4 = 0; num4 < timetableBranch.stations.Count; num4++)
		{
			TimetableStation timetableStation = timetableBranch.stations[num4];
			if (!dictionary.TryGetValue(timetableStation.code, out var value))
			{
				continue;
			}
			bool flag = false;
			for (int num5 = 0; num5 < num4; num5++)
			{
				if (selectedStations.Contains(timetableBranch.stations[num5].code))
				{
					flag = true;
					break;
				}
			}
			bool flag2 = false;
			for (int num6 = num4 + 1; num6 < timetableBranch.stations.Count; num6++)
			{
				if (selectedStations.Contains(timetableBranch.stations[num6].code))
				{
					flag2 = true;
					break;
				}
			}
			GetIndexOfBranchJunctionOnMain(timetableBranch, value, out var junctionEnd2);
			bool num7 = flag && flag2;
			bool flag3 = junctionEnd2 == train.Direction && ((train.Direction == Timetable.Direction.East && flag2) || (train.Direction == Timetable.Direction.West && flag));
			if (!(num7 || flag3))
			{
				continue;
			}
			foreach (TimetableStation station2 in value.stations)
			{
				if (!station2.IsBranchJunctionDuplicate)
				{
					hashSet.Add(station2.code);
				}
			}
		}
		return hashSet;
	}

	private static int GetIndexOfBranchJunctionOnMain(TimetableBranch main, TimetableBranch branch, out Timetable.Direction junctionEnd)
	{
		int num = branch.stations.FindIndex((TimetableStation s) => s.IsBranchJunctionDuplicate);
		junctionEnd = ((num == 0) ? Timetable.Direction.East : Timetable.Direction.West);
		string junctionCode = branch.stations[num].code;
		return main.stations.FindIndex((TimetableStation s) => s.code == junctionCode);
	}
}
