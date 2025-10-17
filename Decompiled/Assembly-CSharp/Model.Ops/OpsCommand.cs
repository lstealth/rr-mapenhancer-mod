using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Game;
using Game.Messages;
using Game.State;
using UI.Console;

namespace Model.Ops;

[StructLayout(LayoutKind.Sequential, Size = 1)]
[ConsoleCommandHandler("ops", "Management of freight and passenger operations.")]
public struct OpsCommand
{
	[ConsoleSubcommand(null, "Move cars to their destinations.")]
	private static string Sweep(string query)
	{
		if (!StateManager.IsHost)
		{
			StateManager.ApplyLocal(new RequestOps(RequestOps.Command.Sweep, query));
			return "Requested ops sweep.";
		}
		OpsController shared = OpsController.Shared;
		if (shared == null)
		{
			return "Missing OpsController.";
		}
		return shared.Sweep(query);
	}

	[ConsoleSubcommand(null, "Offset the number of passengers waiting at a stop.")]
	private static string PassOffset(string stopIdentifier, string origin, string destination, int offset)
	{
		StateManager.AssertIsHost();
		Dictionary<string, PassengerStop> dictionary = PassengerStop.FindAll().ToDictionary((PassengerStop ps) => ps.identifier, (PassengerStop ps) => ps);
		if (!dictionary.TryGetValue(stopIdentifier, out var value))
		{
			throw new Exception("Passenger stop not found: " + stopIdentifier);
		}
		if (!dictionary.ContainsKey(origin))
		{
			throw new Exception("Passenger stop not found: " + origin);
		}
		if (!dictionary.ContainsKey(destination))
		{
			throw new Exception("Passenger stop not found: " + destination);
		}
		value.OffsetWaitingOpsCommand(destination, origin, TimeWeather.Now, offset);
		return GenerateWaitingString(value);
	}

	[ConsoleSubcommand(null, "Display the number of passengers waiting at a stop.")]
	private static string PassWaiting(string stopIdentifier)
	{
		if (!PassengerStop.FindAll().ToDictionary((PassengerStop ps) => ps.identifier, (PassengerStop ps) => ps).TryGetValue(stopIdentifier, out var value))
		{
			throw new Exception("Passenger stop not found: " + stopIdentifier);
		}
		return GenerateWaitingString(value);
	}

	[ConsoleSubcommand(null, "List all active passenger stops.")]
	private static string PassStops()
	{
		List<PassengerStop> list = (from ps in PassengerStop.FindAll()
			where !ps.ProgressionDisabled
			orderby ps.identifier
			select ps).ToList();
		StringBuilder stringBuilder = new StringBuilder();
		foreach (PassengerStop item in list)
		{
			stringBuilder.AppendLine($"{item.identifier} ({item.timetableCode}): {item.Waiting.Count} waiting");
		}
		return stringBuilder.ToString();
	}

	private static string GenerateWaitingString(PassengerStop passengerStop)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("Passengers waiting at " + passengerStop.identifier + " (" + passengerStop.timetableCode + "):");
		GameDateTime now = TimeWeather.Now;
		foreach (KeyValuePair<string, PassengerStop.WaitingInfo> item in passengerStop.Waiting)
		{
			item.Deconstruct(out var key, out var value);
			string text = key;
			PassengerStop.WaitingInfo waitingInfo = value;
			stringBuilder.AppendLine("  Destination: " + text);
			foreach (WaitingPassengerGroup group in waitingInfo.Groups)
			{
				stringBuilder.AppendLine($"    {group.Count} from {group.Origin}, waiting {group.Boarded.IntervalString(now, GameDateTimeInterval.Style.Short)}");
			}
		}
		return stringBuilder.ToString();
	}

	[ConsoleSubcommand("list", "List all industries whose identifier contains the given string.")]
	private static string ListCommand(string query)
	{
		OpsController shared = OpsController.Shared;
		return string.Join("\n", from i in shared.AllIndustries
			where !i.ProgressionDisabled && i.identifier.Contains(query)
			select i.identifier into i
			orderby i
			select i);
	}

	[ConsoleSubcommand(null, "Set the contract tier for an industry.")]
	private string SetTier(string industryId, int tier)
	{
		if (!StateManager.IsSandbox)
		{
			return "Only available in sandbox.";
		}
		Industry industry = OpsController.Shared.AllIndustries.FirstOrDefault((Industry i) => i.identifier == industryId);
		if (industry == null)
		{
			return "Industry not found.";
		}
		if (tier > 0)
		{
			industry.Contract = new Contract(tier);
			return $"Tier {tier} set.";
		}
		industry.Contract = null;
		return "Contract cleared.";
	}

	[ConsoleSubcommand(null, "Find all waybills whose origin or destination identifier contains the given string.")]
	private static string FindWaybills(string query)
	{
		GameDateTime now = TimeWeather.Now;
		IOrderedEnumerable<IGrouping<int, (Car, Waybill)>> source = from tuple in OpsController.Shared.GetOpenWaybills()
			where Matches(tuple.Waybill)
			group tuple by (int)now.DaysSince(tuple.Waybill.Created) into g
			orderby g.Key
			select g;
		return string.Join("\n", source.Select((IGrouping<int, (Car Car, Waybill Waybill)> g) => $"{g.Key} Days: " + string.Join(", ", g.Select(((Car Car, Waybill Waybill) tuple) => tuple.Car.DisplayName))));
		bool Matches(Waybill waybill)
		{
			if (!waybill.Destination.Identifier.Contains(query))
			{
				if (waybill.Origin.HasValue)
				{
					return waybill.Origin.Value.Identifier.Contains(query);
				}
				return false;
			}
			return true;
		}
	}
}
