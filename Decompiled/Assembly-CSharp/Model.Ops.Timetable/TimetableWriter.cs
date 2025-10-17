using System;
using System.Text;
using UnityEngine;

namespace Model.Ops.Timetable;

public static class TimetableWriter
{
	public static string Write(Timetable timetable)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"// Railroader Timetable v{Application.version} - {DateTime.Now}");
		foreach (Timetable.Train value in timetable.Trains.Values)
		{
			WriteTrain(stringBuilder, value);
		}
		return stringBuilder.ToString();
	}

	private static void WriteTrain(StringBuilder sb, Timetable.Train train)
	{
		string text = ((train.Direction == Timetable.Direction.West) ? "W" : "E");
		string text2 = ((int)(train.TrainClass + 1)).ToString();
		string text3 = ((train.TrainType == Timetable.TrainType.Freight) ? "F" : "P");
		sb.Append(train.Name + " " + text + " " + text2 + text3 + ": ");
		for (int i = 0; i < train.Entries.Count; i++)
		{
			Timetable.Entry entry = train.Entries[i];
			sb.Append(entry.Station + " ");
			TimetableTime? arrivalTime = entry.ArrivalTime;
			if (arrivalTime.HasValue)
			{
				TimetableTime valueOrDefault = arrivalTime.GetValueOrDefault();
				WriteTime(sb, valueOrDefault);
				sb.Append("-");
			}
			WriteTime(sb, entry.DepartureTime);
			if (entry.Meets.Count > 0)
			{
				string text4 = string.Join(",", entry.Meets);
				if (!string.IsNullOrEmpty(text4))
				{
					sb.Append(" (" + text4 + ")");
				}
			}
			if (i < train.Entries.Count - 1)
			{
				sb.Append(", ");
			}
		}
		sb.AppendLine();
	}

	private static void WriteTime(StringBuilder sb, TimetableTime time)
	{
		sb.Append(time.IsAbsolute ? time.TimeString() : $"+{time.Minutes}");
	}
}
