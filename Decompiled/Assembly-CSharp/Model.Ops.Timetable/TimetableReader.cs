using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Core.Diagnostics;
using JetBrains.Annotations;
using Markroader;
using Serilog;

namespace Model.Ops.Timetable;

public static class TimetableReader
{
	private static readonly Regex TrainSymbolRegex = new Regex("^[A-Z0-9\\-]+$");

	private static readonly Regex TrainLinePrefixRegex = new Regex("^([A-Z0-9\\-]+) (E|W) (\\w)(\\w):\\s+");

	private static readonly Regex SeparatorOrEndRegex = new Regex("\\s*(?:,|(?:// .*)?$)");

	private static readonly Regex WhitespaceRegex = new Regex("\\s*");

	private static readonly Regex StationCodeRegex = new Regex("^([A-Za-z]+)");

	private static readonly Regex RelativeMinutesRegex = new Regex("^\\+(\\d+)*");

	private static readonly Regex RelativeTimeRegex = new Regex("^\\+(\\d+:\\d{2})");

	private static readonly Regex DepartTimeSeparatorRegex = new Regex("^\\-");

	private static readonly Regex AbsoluteTimeRegex = new Regex("^(\\d+:\\d{2})");

	private static readonly Regex MeetRegex = new Regex("^\\(([A-Z0-9\\-\\s,]*?)\\)");

	private static readonly Regex StationStopMeetRegex = new Regex("^([A-Za-z]+) (\\d+:\\d{2})(?:\\s\\((\\w+)\\))?\\s*,?\\s*");

	private static readonly Regex StationStopADMeetRegex = new Regex("^([A-Za-z]+) (\\d+:\\d{2})-(\\d+:\\d{2})(?:\\s\\((\\w+)\\))?\\s*,?\\s*");

	private static readonly Regex TimeRegex = new Regex("^(\\d+):(\\d{2})$");

	private static readonly Regex CommentRegex = new Regex("^// (.*)$");

	public static bool TryRead(string document, [CanBeNull] IReadOnlyCollection<string> validStationCodes, out Timetable output, [CanBeNull] IDiagnosticCollector diagnostics)
	{
		output = new Timetable(new Dictionary<string, Timetable.Train>());
		StringSlice stringSlice = new StringSlice(document);
		int num = 1;
		bool result = true;
		while (!stringSlice.IsEmpty)
		{
			try
			{
				if (ReadTrainLine(stringSlice.ReadLine(), out var train))
				{
					if (output.Trains.TryGetValue(train.Name, out var _))
					{
						throw new Exception("Train names must be unique: " + train.Name);
					}
					if (validStationCodes != null)
					{
						foreach (Timetable.Entry entry in train.Entries)
						{
							if (!validStationCodes.Contains(entry.Station))
							{
								throw new Exception("Unknown station: " + entry.Station);
							}
						}
					}
					output.Trains[train.Name] = train;
				}
			}
			catch (Exception ex)
			{
				diagnostics?.Log($"Line {num}: {ex.Message}");
				Log.Error(ex, "Line {line}: {message}", num, ex);
				result = false;
			}
			num++;
		}
		return result;
	}

	public static bool IsValidTrainSymbol(string trainSymbol)
	{
		return TrainSymbolRegex.IsMatch(trainSymbol);
	}

	private static bool ReadTrainLine(StringSlice line, out Timetable.Train train)
	{
		train = null;
		if (line.IsEmpty)
		{
			return false;
		}
		if (line.ReadRegex(CommentRegex, out var _))
		{
			return false;
		}
		if (!line.ReadRegex(TrainLinePrefixRegex, out var group, out var group2, out var group3, out var group4))
		{
			throw new Exception("Unexpected line prefix - expected train (#), comment (//), or empty");
		}
		Timetable.TrainClass trainClass = group3.ToString() switch
		{
			"1" => Timetable.TrainClass.First, 
			"2" => Timetable.TrainClass.Second, 
			"3" => Timetable.TrainClass.Third, 
			_ => throw new Exception("Invalid class, expected '1', '2' or '3'."), 
		};
		string text = group4.ToString();
		Timetable.TrainType trainType;
		if (!(text == "P"))
		{
			if (!(text == "F"))
			{
				throw new Exception("Invalid type, expected 'P' or 'F'");
			}
			trainType = Timetable.TrainType.Freight;
		}
		else
		{
			trainType = Timetable.TrainType.Passenger;
		}
		Timetable.TrainType trainType2 = trainType;
		train = new Timetable.Train(group.ToString(), (group2.ToString() == "E") ? Timetable.Direction.East : Timetable.Direction.West, trainClass, trainType2, new List<Timetable.Entry>());
		while (!line.IsEmpty)
		{
			line.ReadRegex(WhitespaceRegex);
			if (!line.ReadRegex(StationCodeRegex, out var slice2))
			{
				throw new Exception(train.Name + ": Expected station code");
			}
			string text2 = $"Train {train.Name} @ {slice2}";
			line.ReadRegex(WhitespaceRegex);
			TimetableTime time;
			StringSlice slice4;
			if (line.ReadRegex(AbsoluteTimeRegex, out var slice3))
			{
				if (!TryParseTime(slice3.ToString(), out time))
				{
					throw new Exception($"{text2}: Invalid time: {slice3}");
				}
			}
			else if (line.ReadRegex(RelativeTimeRegex, out slice4))
			{
				if (!TryParseTime(slice4.ToString(), out var time2))
				{
					throw new Exception($"{text2}: Invalid relative time: {slice4}");
				}
				time = TimetableTime.Relative(time2.Minutes);
			}
			else
			{
				if (!line.ReadRegex(RelativeMinutesRegex, out var slice5))
				{
					throw new Exception("Couldn't find time for entry");
				}
				if (!int.TryParse(slice5.ToString(), out var result))
				{
					throw new Exception($"{text2}: Invalid relative minutes: {slice5}");
				}
				time = TimetableTime.Relative(result);
			}
			TimetableTime? arrivalTime;
			TimetableTime time3;
			if (line.ReadRegex(DepartTimeSeparatorRegex))
			{
				arrivalTime = time;
				if (line.ReadRegex(AbsoluteTimeRegex, out var slice6))
				{
					if (!TryParseTime(slice6.ToString(), out time3))
					{
						throw new Exception($"{text2}: Invalid departure time: {slice6}");
					}
				}
				else if (line.ReadRegex(RelativeTimeRegex, out slice6))
				{
					if (!TryParseTime(slice6.ToString(), out var time4))
					{
						throw new Exception($"{text2}: Invalid departure time: {slice6}");
					}
					time3 = TimetableTime.Relative(time4.Minutes);
				}
				else
				{
					if (!line.ReadRegex(RelativeMinutesRegex, out var slice7))
					{
						throw new Exception(text2 + ": Expected departure time");
					}
					if (!int.TryParse(slice7.ToString(), out var result2))
					{
						throw new Exception($"{text2}: Invalid relative minutes: {slice7}");
					}
					time3 = TimetableTime.Relative(result2);
				}
			}
			else
			{
				arrivalTime = null;
				time3 = time;
			}
			line.ReadRegex(WhitespaceRegex);
			StringSlice slice8;
			IReadOnlyList<string> meets = ((!line.ReadRegex(MeetRegex, out slice8)) ? Array.Empty<string>() : SplitMeets(slice8.ToString()));
			train.Entries.Add(new Timetable.Entry(slice2.ToString(), arrivalTime, time3, meets));
			if (!line.ReadRegex(SeparatorOrEndRegex))
			{
				throw new Exception("Expected comma or end of line");
			}
		}
		return true;
	}

	public static bool TryParseTime(string timeString, out TimetableTime time)
	{
		time = TimetableTime.Absolute(-1);
		Match match = TimeRegex.Match(timeString);
		if (!match.Success)
		{
			return false;
		}
		string value = match.Groups[1].Value;
		string value2 = match.Groups[2].Value;
		if (!int.TryParse(value, out var result) || !int.TryParse(value2, out var result2))
		{
			return false;
		}
		time.Minutes = result * 60 + result2;
		return true;
	}

	private static IReadOnlyList<string> SplitMeets(string meetsString)
	{
		meetsString = meetsString.Trim();
		if (string.IsNullOrEmpty(meetsString))
		{
			return Array.Empty<string>();
		}
		return Regex.Split(meetsString, "\\s*,\\s*");
	}

	public static bool IsValidMeetString(string meets)
	{
		if (string.IsNullOrEmpty(meets))
		{
			return true;
		}
		foreach (string item in SplitMeets(meets))
		{
			if (!IsValidTrainSymbol(item.Trim()))
			{
				return false;
			}
		}
		return true;
	}
}
