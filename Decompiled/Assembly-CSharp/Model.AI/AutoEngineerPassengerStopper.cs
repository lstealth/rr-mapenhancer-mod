using System;
using System.Collections.Generic;
using Core;
using Game;
using Game.Messages;
using Game.Notices;
using Game.State;
using Model.Ops;
using Model.Ops.Timetable;
using RollingStock;
using Serilog;
using UnityEngine;

namespace Model.AI;

public class AutoEngineerPassengerStopper : AutoEngineerComponentBase
{
	private float? _nextStopDistance;

	private string _nextStopText;

	private PassengerStop _nextStop;

	private GameDateTime? _pendingDepartureTimetableTime;

	private string _pendingDepartureTimetableCode;

	private List<Car> _coupledCars = new List<Car>();

	private readonly HashSet<string> _cachedPassengerStopIds = new HashSet<string>();

	private bool _cachedHasCoaches;

	private bool _wasStopped;

	private GameDateTime _stopAnnounceTime;

	private GameDateTime _departAnnounceTime;

	private GameDateTime _timetableWaitAnnounceTime;

	private int _lastMarkersHashCode;

	private GameDateTime _markersHashChanged;

	private bool _didStartBell;

	private GameDateTime _arrivalTime;

	private const float AnnounceTimeout = 60f;

	private const float TimetableWaitAnnounceTimeout = 1800f;

	private const float MarkersHashChangeTimeout = 5f;

	private const float ArrivalBellDistance = 100f;

	private const float StopRadius = 10f;

	private Timetable.Train _timetableTrain;

	public (float distance, string text, string bypassTimetableCode)? NextStopInfo
	{
		get
		{
			if (_nextStopDistance.HasValue)
			{
				return (_nextStopDistance.Value, _nextStopText, _pendingDepartureTimetableCode);
			}
			return null;
		}
	}

	public bool IsStoppedAtStation
	{
		get
		{
			(float, string, string)? nextStopInfo = NextStopInfo;
			if (nextStopInfo.HasValue)
			{
				return nextStopInfo.GetValueOrDefault().Item1 < 0.1f;
			}
			return false;
		}
	}

	private static GameDateTime Now => TimeWeather.Now;

	private static bool Enable => StateManager.Shared.Storage.AIPassengerStopEnable;

	private static float MinimumStopDuration => StateManager.Shared.Storage.AIPassengerStopMinimumStopDuration;

	private bool IsTimetableTrain => _timetableTrain != null;

	private void OnDisable()
	{
		StopBell();
	}

	public void SetTimetableTrain(Timetable.Train train)
	{
		_timetableTrain = train;
	}

	public void UpdateCars(List<Car> coupledCars)
	{
		_coupledCars = coupledCars;
		_cachedPassengerStopIds.Clear();
		_cachedHasCoaches = false;
		foreach (Car coupledCar in coupledCars)
		{
			_cachedHasCoaches = _cachedHasCoaches || coupledCar.IsPassengerCar();
			if (TryGetMarker(coupledCar, out var marker))
			{
				_cachedPassengerStopIds.UnionWith(marker.Destinations);
			}
		}
	}

	private static bool TryGetMarker(Car car, out PassengerMarker marker)
	{
		marker = default(PassengerMarker);
		if (!car.IsPassengerCar())
		{
			return false;
		}
		PassengerMarker? passengerMarker = car.GetPassengerMarker();
		if (!passengerMarker.HasValue)
		{
			return false;
		}
		marker = passengerMarker.Value;
		return true;
	}

	internal void UpdateFor(AutoEngineerPlanner.Found<PassengerStop>? maybeAhead, AutoEngineerPlanner.Found<PassengerStop>? maybeUnder, float? stoppedDuration, string bypassStationCode)
	{
		_UpdateFor(maybeAhead, maybeUnder, stoppedDuration, bypassStationCode);
		string passengerModeStatus = null;
		if (Enable && _cachedHasCoaches)
		{
			passengerModeStatus = ((_cachedPassengerStopIds.Count > 0) ? "Stop when passing" : "No stops planned");
		}
		base.Planner.Persistence.PassengerModeStatus = passengerModeStatus;
	}

	private void _UpdateFor(AutoEngineerPlanner.Found<PassengerStop>? maybeAhead, AutoEngineerPlanner.Found<PassengerStop>? maybeUnder, float? stoppedDuration, string bypassStationCode)
	{
		bool flag = stoppedDuration > 1f;
		bool flag2 = flag && !_wasStopped;
		_wasStopped = flag;
		if (flag && _nextStop != null && _nextStopDistance.HasValue && _nextStopDistance.Value < 10f)
		{
			if (flag2)
			{
				MarkArrival();
				StopBell();
			}
			if (ShouldStayStopped(bypassStationCode) && Enable)
			{
				return;
			}
		}
		_nextStopDistance = null;
		_nextStop = null;
		float? num = null;
		if (!Enable)
		{
			return;
		}
		if (maybeUnder.HasValue)
		{
			AutoEngineerPlanner.Found<PassengerStop> valueOrDefault = maybeUnder.GetValueOrDefault();
			if (valueOrDefault.Item.timetableCode != bypassStationCode)
			{
				PassengerStop item = valueOrDefault.Item;
				if (IsTimetableTrain)
				{
					if (ShouldStopPerTimetable(item, out var departureTime))
					{
						float num2 = FindStopDistanceForIdentifier(item.identifier);
						SetNextStopTimetable(item, num2 - valueOrDefault.Distance, departureTime);
						num = 0f;
					}
				}
				else if (_cachedPassengerStopIds.Contains(item.identifier))
				{
					float num3 = FindStopDistanceForIdentifier(item.identifier) - valueOrDefault.Distance;
					Log.Debug("FindStopDistance: {stop} at distance {distance}, nsd {nextStopDistance}", item.DisplayName, valueOrDefault.Distance, num3);
					if (num3 > -10f)
					{
						SetNextStopStation(item, num3);
						num = 0f;
					}
					else
					{
						Log.Debug("Ignore passed stop {stop} at distance {distance}", item, valueOrDefault.Distance);
					}
				}
			}
		}
		if (maybeAhead.HasValue)
		{
			AutoEngineerPlanner.Found<PassengerStop> valueOrDefault2 = maybeAhead.GetValueOrDefault();
			if (valueOrDefault2.Item.timetableCode != bypassStationCode && !_nextStopDistance.HasValue)
			{
				PassengerStop item2 = valueOrDefault2.Item;
				if (IsTimetableTrain)
				{
					if (ShouldStopPerTimetable(item2, out var departureTime2))
					{
						float num4 = FindStopDistanceForIdentifier(item2.identifier);
						SetNextStopTimetable(item2, num4 + valueOrDefault2.Distance, departureTime2);
						num = valueOrDefault2.Distance;
					}
				}
				else if (_cachedPassengerStopIds.Contains(item2.identifier))
				{
					float num5 = FindStopDistanceForIdentifier(item2.identifier);
					SetNextStopStation(item2, num5 + valueOrDefault2.Distance);
					num = valueOrDefault2.Distance;
				}
			}
		}
		float velocityMphAbs = _locomotive.VelocityMphAbs;
		if (num.HasValue && num < 100f && velocityMphAbs > 1f)
		{
			SoundBell();
		}
		else
		{
			StopBell();
		}
	}

	private void SetNextStopStation(PassengerStop passengerStop, float distance)
	{
		if (IsTimetableStop(passengerStop, out var departureTime))
		{
			SetNextStopTimetable(passengerStop, distance, departureTime, isStationStop: true);
			return;
		}
		_nextStopDistance = distance;
		_nextStop = passengerStop;
		_nextStopText = GetStationStopText(passengerStop);
		_pendingDepartureTimetableCode = null;
		_pendingDepartureTimetableTime = null;
	}

	private void SetNextStopTimetable(PassengerStop passengerStop, float distance, GameDateTime departureTime, bool isStationStop = false)
	{
		_nextStopDistance = distance;
		_nextStop = passengerStop;
		_nextStopText = (isStationStop ? GetStationStopText(passengerStop) : ("Timetable: Depart " + passengerStop.TimetableName + " " + departureTime.TimeString()));
		_pendingDepartureTimetableCode = passengerStop.timetableCode;
		_pendingDepartureTimetableTime = departureTime;
	}

	private static string GetStationStopText(PassengerStop passengerStop)
	{
		return "Station Stop: " + passengerStop.DisplayName;
	}

	private float GetTrainLength()
	{
		float num = 0f;
		foreach (Car coupledCar in _coupledCars)
		{
			num += coupledCar.carLength + 1f;
		}
		return num;
	}

	private void SoundBell()
	{
		if (!_locomotive.ControlHelper.Bell)
		{
			Log.Debug("Bell: Start");
			_locomotive.ControlHelper.Bell = true;
			_didStartBell = true;
		}
	}

	private void StopBell()
	{
		if (_didStartBell)
		{
			Log.Debug("Bell: Stop");
			_locomotive.ControlHelper.Bell = false;
			_didStartBell = false;
		}
	}

	private int CalculateMarkersHash()
	{
		int num = 0;
		foreach (Car coupledCar in _coupledCars)
		{
			if (TryGetMarker(coupledCar, out var marker))
			{
				num = HashCode.Combine(num, marker.PropertyValue().GetHashCode());
			}
		}
		return num;
	}

	private void MarkArrival()
	{
		_arrivalTime = Now;
		if (_stopAnnounceTime + 60f < Now)
		{
			_stopAnnounceTime = Now;
			Say($"Arrived at {Hyperlink.To(_nextStop)}.");
		}
	}

	private bool ShouldStayStopped(string bypassStationCode)
	{
		GameDateTime now = Now;
		int num = CalculateMarkersHash();
		if (num != _lastMarkersHashCode)
		{
			_markersHashChanged = now;
			_lastMarkersHashCode = num;
		}
		if (now - _arrivalTime < (double)MinimumStopDuration)
		{
			return true;
		}
		if (IsLastTimetableStop(_nextStop))
		{
			Orders orders = base.Planner.Persistence.Orders;
			if (orders.Mode == AutoEngineerMode.Road && orders.MaxSpeedMph > 0)
			{
				Say($"Holding at {Hyperlink.To(_nextStop)}; train schedule is complete.");
				base.Planner.Persistence.Orders = orders.WithMaxSpeedMph(0);
				_locomotive.PostNotice("ai-tt-complete", "Timetable schedule complete.");
				return false;
			}
		}
		if (_markersHashChanged + 5f > now)
		{
			return true;
		}
		if (ShouldStopPerTimetable(_nextStop, out var departureTime) && _nextStop.timetableCode != bypassStationCode)
		{
			SetNextStopTimetable(_nextStop, 0f, departureTime);
			if (_timetableWaitAnnounceTime + 1800f < now && departureTime - now > 120.0)
			{
				_timetableWaitAnnounceTime = now;
				Say($"Holding at {Hyperlink.To(_nextStop)} until {departureTime.TimeString()}.");
			}
			return true;
		}
		_timetableWaitAnnounceTime = default(GameDateTime);
		if (_departAnnounceTime + 60f < now)
		{
			_departAnnounceTime = now;
			GameDateTime? pendingDepartureTimetableTime = _pendingDepartureTimetableTime;
			if (pendingDepartureTimetableTime.HasValue)
			{
				GameDateTime valueOrDefault = pendingDepartureTimetableTime.GetValueOrDefault();
				GameDateTime gameDateTime = now.StartOfDay.AddingDays(-1f);
				int num2 = Mathf.FloorToInt((float)(now - gameDateTime) / 60f);
				int num3 = Mathf.FloorToInt((float)(valueOrDefault - gameDateTime) / 60f);
				string earlyLateString = GetEarlyLateString(num2 - num3);
				Say($"Departing {Hyperlink.To(_nextStop)} {earlyLateString}.");
			}
			else
			{
				Say($"Departing {Hyperlink.To(_nextStop)}.");
			}
		}
		return false;
	}

	private static string GetEarlyLateString(int offsetMinutes)
	{
		if (offsetMinutes <= 0)
		{
			if (offsetMinutes == 0)
			{
				return "on time";
			}
			return (-offsetMinutes).Pluralize("minute") + " early";
		}
		return offsetMinutes.Pluralize("minute") + " late";
	}

	private bool TryGetTimetableEntry(PassengerStop passengerStop, out Timetable.Entry entry, out int entryIndex)
	{
		entry = default(Timetable.Entry);
		entryIndex = -1;
		if (_timetableTrain != null && !string.IsNullOrEmpty(passengerStop.timetableCode))
		{
			return _timetableTrain.TryGetTimetableEntry(passengerStop.timetableCode, out entry, out entryIndex);
		}
		return false;
	}

	private bool IsLastTimetableStop(PassengerStop passengerStop)
	{
		if (_timetableTrain == null)
		{
			return false;
		}
		if (!TryGetTimetableEntry(passengerStop, out var _, out var entryIndex))
		{
			return false;
		}
		return entryIndex == _timetableTrain.Entries.Count - 1;
	}

	private bool ShouldStopPerTimetable(PassengerStop passengerStop, out GameDateTime departureTime)
	{
		if (!IsTimetableStop(passengerStop, out departureTime))
		{
			return false;
		}
		if (Now < departureTime)
		{
			return true;
		}
		return _cachedPassengerStopIds.Contains(passengerStop.identifier);
	}

	private bool IsTimetableStop(PassengerStop passengerStop, out GameDateTime departureTime)
	{
		departureTime = default(GameDateTime);
		if (!TryGetTimetableEntry(passengerStop, out var _, out var entryIndex))
		{
			return false;
		}
		GameDateTime now = Now;
		departureTime = _timetableTrain.GetGameDateTimeDeparture(entryIndex, now);
		return true;
	}

	private float FindStopDistanceForIdentifier(string passengerStopIdentifier)
	{
		float num = 0f;
		float? num2 = null;
		foreach (Car coupledCar in _coupledCars)
		{
			PassengerMarker marker;
			bool flag = TryGetMarker(coupledCar, out marker) && marker.Destinations.Contains(passengerStopIdentifier);
			if (flag && !num2.HasValue)
			{
				num2 = num;
			}
			else if (!flag && num2.HasValue)
			{
				break;
			}
			num += coupledCar.carLength + 1f;
		}
		if (num2.HasValue)
		{
			return Mathf.Lerp(num2.Value, num, 0.5f);
		}
		num = 0f;
		num2 = null;
		foreach (Car coupledCar2 in _coupledCars)
		{
			bool flag2 = coupledCar2.IsPassengerCar();
			if (flag2 && !num2.HasValue)
			{
				num2 = num;
			}
			else if (!flag2 && num2.HasValue)
			{
				break;
			}
			num += coupledCar2.carLength + 1f;
		}
		if (num2.HasValue)
		{
			return Mathf.Lerp(num2.Value, num, 0.5f);
		}
		return GetTrainLength() / 2f;
	}

	public override void ApplyMovement(MovementInfo info)
	{
		if (_nextStopDistance.HasValue)
		{
			_nextStopDistance -= info.Distance;
			_nextStopDistance = Mathf.Max(0f, _nextStopDistance.Value);
		}
	}

	public override void WillMove()
	{
		_nextStop = null;
		_nextStopDistance = null;
		_stopAnnounceTime = default(GameDateTime);
	}
}
