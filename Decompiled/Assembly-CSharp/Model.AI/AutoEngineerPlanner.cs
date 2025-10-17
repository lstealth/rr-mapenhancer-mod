using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core;
using GalaSoft.MvvmLight.Messaging;
using Game;
using Game.Events;
using Game.Messages;
using Game.Notices;
using Game.State;
using Helpers;
using Model.Definition;
using Model.Ops;
using Model.Ops.Timetable;
using Model.Physics;
using Network;
using Network.Messages;
using RollingStock;
using Serilog;
using Track;
using Track.Search;
using Track.Signals;
using UnityEngine;
using UnityEngine.Pool;

namespace Model.AI;

public class AutoEngineerPlanner : MonoBehaviour
{
	private struct SignalSpeedRestriction
	{
		public int SpeedMph;

		public readonly int? ApproachSpeedMph;

		public float DistanceToSignal;

		public readonly string SignalId;

		public float DistanceLimit;

		public SignalSpeedRestriction(int mph, int? approachSpeedMph, float distanceToSignal, string signalId, float distanceLimit)
		{
			SpeedMph = mph;
			ApproachSpeedMph = approachSpeedMph;
			DistanceToSignal = distanceToSignal;
			SignalId = signalId;
			DistanceLimit = distanceLimit;
		}
	}

	private enum OtherCarBehavior
	{
		Couple,
		NoCouple,
		Avoid
	}

	private readonly struct OtherCarHandling
	{
		public readonly OtherCarBehavior Behavior;

		public readonly string CarId;

		public bool Couple => Behavior == OtherCarBehavior.Couple;

		public static OtherCarHandling Avoid => new OtherCarHandling(OtherCarBehavior.Avoid, null);

		public static OtherCarHandling NoCouple => new OtherCarHandling(OtherCarBehavior.NoCouple, null);

		private OtherCarHandling(OtherCarBehavior behavior, string carId)
		{
			Behavior = behavior;
			CarId = carId;
		}

		public static OtherCarHandling CoupleTo(string carId)
		{
			return new OtherCarHandling(OtherCarBehavior.Couple, carId);
		}
	}

	private enum SwitchAgainstHandling
	{
		StopBeforeFouling,
		FoulThrowableSwitches
	}

	private struct TargetInfo
	{
		public AutoEngineer.Targets.Target Target;

		public StopAnnounce? StopAnnounce;
	}

	private enum SetSwitchResult
	{
		Occupied,
		CTC,
		Success
	}

	private enum SearchMode
	{
		Ahead,
		Self
	}

	private enum DistanceLimiter
	{
		Other,
		Car
	}

	private struct SearchResult
	{
		public float AvailableDistance;

		public string AvailableDistanceReason;

		public float MaxSpeedMph;

		public float MaxSpeedMphNear;

		public Found<CTCSignal>? NextSignal;

		public Found<string>? NextFlare;

		public float? NextCrossingDistance;

		public Found<PassengerStop>? NextPassengerStop;

		public float NextRestrictionDistance;

		public float AverageGrade;

		public DistanceLimiter DistanceLimiter;

		public StopAnnounce? StopAnnounce;

		public float LimitingCarRelativeVelocity;
	}

	internal struct Found<TItem>
	{
		public readonly TItem Item;

		public readonly Location Location;

		public readonly float Distance;

		public Found(TItem item, Location location, float distance)
		{
			Item = item;
			Location = location;
			Distance = distance;
		}
	}

	private AutoEngineer _engineer;

	private BaseLocomotive _locomotive;

	private Coroutine _coroutine;

	private readonly CoroutineKeepalive _loopKeepalive = AutoEngineer.CreateKeepalive();

	private IDisposable _ordersObserver;

	private readonly List<Car> _coupledCarsCached = new List<Car>();

	private Car.End _coupledCarsCachedEnd;

	private bool _derailed;

	private float _equipmentMaximumTrackCurvature;

	private float _maximumLength;

	private AutoEngineerPersistence _persistence;

	private Orders _orders;

	private float? _manualStopDistance;

	private SignalSpeedRestriction? _lastSignalSpeedRestriction;

	private IDisposable _manualStopObserver;

	private AutoEngineerConfig _config;

	private AutoEngineerFuelAlerter _fuelAlerter;

	private AutoHotboxSpotter _hotboxSpotter;

	private AutoEngineerCrossingSignaler _crossingSignaler;

	private AutoEngineerPassengerStopper _passengerStopper;

	private Serilog.ILogger _log;

	private Graph _graph;

	private TimetableController _timetableController;

	private string _contextualIgnoreSignalId;

	private string _contextualIgnoreFlareId;

	private string _contextualBypassTimetableStation;

	private (string signalId, SignalAspect aspect)? _calledSignal;

	private string _stopAndProceedSignalId;

	[Tooltip("Set true to kill the coroutine and test keepalive.")]
	[SerializeField]
	private bool testKeepalive;

	private Coroutine _routeCoroutine;

	private List<RouteSearch.Step> _route;

	private Location? _routeTargetLocation;

	private int _startStepIndex;

	private IPlayer _routeRequester;

	private Dictionary<SearchState, float> _debugLastSearchCosts;

	private readonly Dictionary<string, bool> _routeSwitchesToRestore = new Dictionary<string, bool>();

	private readonly DictionarySet<int, string> _routeExtraSwitches = new DictionarySet<int, string>();

	private readonly List<Vector3> _underTrainPoints = new List<Vector3>();

	private OrderWaypoint _lastRouteWaypoint;

	private bool _waypointNoticeCleared;

	private float _lastNotCurrentRouteReroute;

	private const float ChangeDirectionPadding = 3f;

	[SerializeField]
	private bool drawRouteGizmos;

	internal ref AutoEngineerPersistence Persistence => ref _persistence;

	internal bool IsYardMode => _orders.Mode == AutoEngineerMode.Yard;

	private float BaseDistanceLimit => Mathf.Max(_maximumLength, 250f);

	private static bool IsCTC
	{
		get
		{
			CTCPanelController shared = CTCPanelController.Shared;
			if (shared == null)
			{
				return false;
			}
			return shared.SystemMode == SystemMode.CTC;
		}
	}

	private void Awake()
	{
		_graph = Graph.Shared;
		_timetableController = TimetableController.Shared;
		_engineer = base.gameObject.AddComponent<AutoEngineer>();
		_locomotive = _engineer.Locomotive;
		_log = Log.ForContext<AutoEngineerPlanner>().ForContext("locomotive", _locomotive.DisplayName);
	}

	private void OnEnable()
	{
		_config = TrainController.Shared.autoEngineerConfig;
		_persistence = new AutoEngineerPersistence(_locomotive.KeyValueObject);
		_manualStopObserver = _persistence.ObserveManualStopDistance(delegate(float? dist)
		{
			_manualStopDistance = dist;
		});
		_ordersObserver = _persistence.ObserveOrders(delegate(Orders orders)
		{
			_orders = orders;
			OrdersDidChange();
		});
		Messenger.Default.Register<WorldWillSave>(this, delegate
		{
			_persistence.ManualStopDistance = _manualStopDistance;
		});
		Messenger.Default.Register<TimetableDidChange>(this, delegate
		{
			UpdateTimetableTrain();
		});
	}

	private void OnDisable()
	{
		if (_coroutine != null)
		{
			StopCoroutineAndDestroyChildComponents();
		}
		_ordersObserver?.Dispose();
		_manualStopObserver?.Dispose();
		Messenger.Default.Unregister(this);
	}

	private void OnDrawGizmos()
	{
		DrawRouteGizmos();
	}

	private void OrdersDidChange()
	{
		_log.Debug("OrdersDidChange: {orders}", _locomotive, _orders);
		UpdateWaypointRouteIfNeeded();
		bool flag = _orders.Enabled;
		bool flag2 = _coroutine != null;
		if (flag != flag2)
		{
			if (flag)
			{
				_crossingSignaler = base.gameObject.AddComponent<AutoEngineerCrossingSignaler>();
				_fuelAlerter = base.gameObject.AddComponent<AutoEngineerFuelAlerter>();
				_hotboxSpotter = base.gameObject.AddComponent<AutoHotboxSpotter>();
				_coroutine = StartCoroutine(Loop());
				_loopKeepalive.Start(this, KeepaliveTimedOut);
			}
			else
			{
				StopCoroutineAndDestroyChildComponents();
				OffDuty();
			}
		}
		bool num = _routeCoroutine != null;
		bool flag3 = _orders.Waypoint.HasValue && flag;
		if (num != flag3)
		{
			if (flag3)
			{
				_routeCoroutine = StartCoroutine(RouteLoop());
				return;
			}
			StopCoroutine(_routeCoroutine);
			_routeCoroutine = null;
		}
	}

	private void KeepaliveTimedOut()
	{
		_log.Warning("Keepalive timed out. Restarting.");
		StopCoroutineAndDestroyChildComponents();
		OrdersDidChange();
	}

	private void StopCoroutineAndDestroyChildComponents()
	{
		if (_coroutine != null)
		{
			StopCoroutine(_coroutine);
		}
		_coroutine = null;
		_loopKeepalive.Stop();
		if (_routeCoroutine != null)
		{
			StopCoroutine(_routeCoroutine);
		}
		_routeCoroutine = null;
		UnityEngine.Object.Destroy(_crossingSignaler);
		_crossingSignaler = null;
		UnityEngine.Object.Destroy(_passengerStopper);
		_passengerStopper = null;
		UnityEngine.Object.Destroy(_fuelAlerter);
		_fuelAlerter = null;
		UnityEngine.Object.Destroy(_hotboxSpotter);
		_hotboxSpotter = null;
	}

	private static IEnumerator WaitFixed(float seconds)
	{
		return AutoEngineer.WaitFixed(seconds);
	}

	private IEnumerator Loop()
	{
		while (true)
		{
			if (_locomotive.set == null || !_orders.Enabled)
			{
				_loopKeepalive.StillAlive();
				OffDuty();
				yield return WaitFixed(0.5f);
				continue;
			}
			UpdateCars(out var delta);
			_loopKeepalive.StillAlive();
			_engineer.Run = true;
			if (_derailed)
			{
				_engineer.SetTargets(new AutoEngineer.Targets());
				yield return WaitFixed(2f);
				_persistence.ClearOrders();
				continue;
			}
			if (delta != 0)
			{
				List<RouteSearch.Step> route = _route;
				if (route != null && route.Count > 0)
				{
					UpdateWaypointRouteIfNeeded(force: true);
				}
			}
			if (delta > 0 && _orders.Mode != AutoEngineerMode.Road)
			{
				_log.Information("Coupled delta in yard more {delta}, stopping", delta);
				_manualStopDistance = 0f;
			}
			_engineer.InvalidateCachedCars();
			if (OrdersWantMovement() && ShouldStopForPitfall(out var pitfallStopReason))
			{
				PostPitfallNotice(pitfallStopReason);
				_engineer.SetTargets(new AutoEngineer.Targets());
				SetPlannerStatus(0f, pitfallStopReason);
				yield return WaitFixed(2f);
				continue;
			}
			PostPitfallNotice(null);
			if (!_locomotive.HasFuel)
			{
				Say((_locomotive.Archetype == CarArchetype.LocomotiveSteam) ? "Check the tender, we're empty." : "All out of fuel.");
				_engineer.SetTargets(new AutoEngineer.Targets());
				yield return WaitFixed(2f);
				_persistence.ClearOrders();
				continue;
			}
			int signedMaxSpeedMph = _orders.SignedMaxSpeedMph;
			float num = Mathf.Sign(signedMaxSpeedMph);
			float num2 = Mathf.Abs(_locomotive.velocity);
			float num3 = num2 * 2.23694f;
			if (num != Mathf.Sign(_locomotive.velocity) && num2 > 0.5f)
			{
				_engineer.SetTargets(new AutoEngineer.Targets(0f, new List<AutoEngineer.Targets.Target>(), 0f, 0f, changeDirection: true, AutoEngineerMode.Road, null, null));
				yield return WaitFixed(2f);
				continue;
			}
			try
			{
				UpdateTargets(num, signedMaxSpeedMph);
			}
			catch (Exception exception)
			{
				_log.Error(exception, "Exception while updating targets");
			}
			finally
			{
			}
			float num4 = ((num3 > 30f) ? 0.5f : ((!(num3 > 0.1f)) ? 3f : 1f));
			float seconds = num4;
			yield return WaitFixed(seconds);
		}
	}

	private void UpdateTargets(float direction, int enabledMaxSpeedMph)
	{
		float num = _engineer.CalculateLookaheadDistance();
		Location start = StartLocation();
		OtherCarHandling otherCarHandling = _orders.Mode switch
		{
			AutoEngineerMode.Off => OtherCarHandling.Avoid, 
			AutoEngineerMode.Road => OtherCarHandling.Avoid, 
			AutoEngineerMode.Yard => OtherCarHandling.CoupleTo(null), 
			AutoEngineerMode.Waypoint => (_orders.Waypoint.HasValue && !string.IsNullOrEmpty(_orders.Waypoint.Value.CoupleToCarId)) ? OtherCarHandling.CoupleTo(_orders.Waypoint.Value.CoupleToCarId) : OtherCarHandling.NoCouple, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
		SwitchAgainstHandling switchAgainstHandling = ((_orders.Mode == AutoEngineerMode.Waypoint) ? SwitchAgainstHandling.FoulThrowableSwitches : SwitchAgainstHandling.StopBeforeFouling);
		float num2 = Mathf.Abs(_locomotive.velocity);
		Search(_coupledCarsCached[0], num2, start, num, SearchMode.Ahead, _equipmentMaximumTrackCurvature, otherCarHandling, switchAgainstHandling, _coupledCarsCached, out var result);
		float availableDistance = result.AvailableDistance;
		string availableDistanceReason = result.AvailableDistanceReason;
		float maxSpeedMph = result.MaxSpeedMph;
		float maxSpeedMphNear = result.MaxSpeedMphNear;
		Found<CTCSignal>? found = result.NextSignal;
		Found<string>? nextFlare = result.NextFlare;
		float? nextCrossingDistance = result.NextCrossingDistance;
		float nextRestrictionDistance = result.NextRestrictionDistance;
		float averageGrade = result.AverageGrade;
		DistanceLimiter distanceLimiter = result.DistanceLimiter;
		StopAnnounce? stopAnnounce = result.StopAnnounce;
		float num3 = availableDistance;
		List<Car> coupledCarsCached = _coupledCarsCached;
		Search(coupledCarsCached[coupledCarsCached.Count - 1], num2, start.Flipped(), _maximumLength, SearchMode.Self, _equipmentMaximumTrackCurvature, OtherCarHandling.Avoid, SwitchAgainstHandling.StopBeforeFouling, _coupledCarsCached, out var result2);
		float maxSpeedMph2 = result2.MaxSpeedMph;
		float averageGradeUnder = result2.AverageGrade * -1f;
		if (otherCarHandling.Couple && stopAnnounce == StopAnnounce.OtherTrain)
		{
			stopAnnounce = null;
		}
		List<TargetInfo> targetInfos = new List<TargetInfo>();
		List<ContextualOrder> contextualOrders = new List<ContextualOrder>();
		if (_contextualIgnoreSignalId == found?.Item.id)
		{
			found = null;
		}
		SignalSpeedRestriction? lastSignalSpeedRestriction;
		if (found.HasValue)
		{
			Found<CTCSignal> valueOrDefault = found.GetValueOrDefault();
			CTCSignal item = valueOrDefault.Item;
			if (valueOrDefault.Distance > availableDistance)
			{
				_lastSignalSpeedRestriction = null;
			}
			else
			{
				float distance = valueOrDefault.Distance;
				SignalAspect lastShownAspect = item.LastShownAspect;
				switch (lastShownAspect)
				{
				case SignalAspect.Stop:
					if (item.IsIntermediate)
					{
						if ((distance < 40f && _locomotive.IsStopped()) || _stopAndProceedSignalId == item.id)
						{
							_stopAndProceedSignalId = item.id;
							SetSignalSpeedRestriction(15, null, distance, item, distanceLimited: true);
						}
						else
						{
							AddTarget(0f, distance - 15f, "Stop and Proceed Signal");
						}
					}
					else
					{
						AddContextualOrder(ContextualOrder.OrderValue.PassSignal, item.id);
						SetSignalSpeedRestriction(0, null, distance, item);
					}
					break;
				case SignalAspect.Clear:
				case SignalAspect.DivergingClear:
					if (!(distance > 200f))
					{
						SetSignalSpeedRestriction(null, null, distance, item);
					}
					break;
				case SignalAspect.Approach:
				case SignalAspect.DivergingApproach:
					if (!(distance > 200f))
					{
						float? num4 = DistanceToNextSignalAfter(valueOrDefault, 2000f);
						if (!num4.HasValue)
						{
							SetSignalSpeedRestriction(25, 25, distance, item, distanceLimited: true);
							break;
						}
						float distanceToSignal = distance + num4.Value;
						SetSignalSpeedRestriction(15, 25, distanceToSignal, item);
					}
					break;
				case SignalAspect.Restricting:
					if (!(distance > 200f))
					{
						SetSignalSpeedRestriction(15, null, distance, item, distanceLimited: true);
					}
					break;
				}
				if (StateManager.Shared.Storage.AICallSignals != 0 && distance < 100f && (!_manualStopDistance.HasValue || _manualStopDistance > distance))
				{
					CallSignalIfNeeded(item, lastShownAspect);
				}
			}
		}
		else
		{
			lastSignalSpeedRestriction = _lastSignalSpeedRestriction;
			if (lastSignalSpeedRestriction.HasValue && lastSignalSpeedRestriction.GetValueOrDefault().DistanceToSignal > 0f)
			{
				_lastSignalSpeedRestriction = null;
			}
		}
		lastSignalSpeedRestriction = _lastSignalSpeedRestriction;
		if (lastSignalSpeedRestriction.HasValue)
		{
			SignalSpeedRestriction restriction = lastSignalSpeedRestriction.GetValueOrDefault();
			if (restriction.SpeedMph == 0)
			{
				AddTarget(restriction.SpeedMph, restriction.DistanceToSignal - 15f, "Stop Signal", StopAnnounce.StopSignal);
			}
			else
			{
				AddTarget(restriction.SpeedMph, restriction.DistanceToSignal, "Signal");
			}
			if (restriction.ApproachSpeedMph.HasValue)
			{
				AddTarget(restriction.ApproachSpeedMph.Value, 0f, "Approach Signal");
			}
			if (!contextualOrders.Any((ContextualOrder co) => co.Order == ContextualOrder.OrderValue.PassSignal && co.Context == restriction.SignalId))
			{
				AddContextualOrder(ContextualOrder.OrderValue.ResumeSpeed, restriction.SignalId);
			}
		}
		if (_contextualIgnoreSignalId != null && result.NextSignal?.Item.id != _contextualIgnoreSignalId)
		{
			_log.Debug("Dropping contextual ignore signal id = {signalId} - not in sight", _contextualIgnoreSignalId);
			_contextualIgnoreSignalId = null;
		}
		if (_contextualIgnoreFlareId != null && nextFlare?.Item != _contextualIgnoreFlareId)
		{
			_log.Debug("Dropping contextual ignore flare id = {flareId} - no flare in sight", _contextualIgnoreFlareId);
			_contextualIgnoreFlareId = null;
		}
		if (nextFlare.HasValue)
		{
			Found<string> valueOrDefault2 = nextFlare.GetValueOrDefault();
			if (_contextualIgnoreFlareId != valueOrDefault2.Item)
			{
				float targetDistance = valueOrDefault2.Distance - 5f;
				AddTarget(0f, targetDistance, "Fusee", StopAnnounce.Fusee);
				AddContextualOrder(ContextualOrder.OrderValue.PassFlare, valueOrDefault2.Item);
			}
		}
		float num5 = Mathf.Abs(enabledMaxSpeedMph);
		string maxSpeedReason = ((num5 < 0.1f) ? "Orders: Stop" : "Orders");
		if (_hotboxSpotter.HotboxSpotted && num5 > 15f)
		{
			num5 = 15f;
			maxSpeedReason = "Hotbox";
		}
		if (maxSpeedMph2 < num5)
		{
			num5 = maxSpeedMph2;
			maxSpeedReason = "Track Speed";
		}
		if (maxSpeedMphNear < num5)
		{
			num5 = maxSpeedMphNear;
			maxSpeedReason = "Track Speed";
		}
		if (distanceLimiter == DistanceLimiter.Car && otherCarHandling.Couple && availableDistance < 50f)
		{
			AddTarget(3f, availableDistance - 5f, "Couple");
		}
		if (availableDistance < num)
		{
			if (distanceLimiter == DistanceLimiter.Car)
			{
				float num6 = num2 * 2.23694f;
				float num7 = result.LimitingCarRelativeVelocity * 2.23694f;
				float targetSpeedMphAbs = Mathf.Clamp(Mathf.Lerp(0f, num6 + num7, Mathf.InverseLerp(-0.1f, 5f, num7)) * Mathf.Lerp(0.8f, 1f, Mathf.InverseLerp(40f, 80f, availableDistance)), 0f, num5);
				AddTarget(targetSpeedMphAbs, availableDistance - 1f, availableDistanceReason, stopAnnounce);
			}
			else
			{
				AddTarget(0f, availableDistance - 1f, availableDistanceReason, stopAnnounce);
			}
		}
		else
		{
			float targetDistance2 = ((maxSpeedMph > 0.1f) ? Mathf.Max(0f, nextRestrictionDistance - 13f) : nextRestrictionDistance);
			AddTarget(maxSpeedMph, targetDistance2, "Track Speed");
		}
		if (_passengerStopper != null)
		{
			if (!string.IsNullOrEmpty(_contextualBypassTimetableStation) && result.NextPassengerStop?.Item.timetableCode != _contextualBypassTimetableStation && result2.NextPassengerStop?.Item.timetableCode != _contextualBypassTimetableStation)
			{
				_contextualBypassTimetableStation = null;
			}
			float? stoppedDuration = _locomotive.StoppedDuration;
			if (IsYardMode)
			{
				_passengerStopper.UpdateFor(null, null, stoppedDuration, _contextualBypassTimetableStation);
			}
			else
			{
				_passengerStopper.UpdateFor(result.NextPassengerStop, result2.NextPassengerStop, stoppedDuration, _contextualBypassTimetableStation);
			}
			(float, string, string)? nextStopInfo = _passengerStopper.NextStopInfo;
			if (nextStopInfo.HasValue)
			{
				(float, string, string) valueOrDefault3 = nextStopInfo.GetValueOrDefault();
				AddTarget(0f, valueOrDefault3.Item1, valueOrDefault3.Item2);
				if (valueOrDefault3.Item3 != null)
				{
					AddContextualOrder(ContextualOrder.OrderValue.BypassTimetable, valueOrDefault3.Item3);
				}
				_log.Debug("PassengerStopper: NextStopDistance = {distance}", valueOrDefault3.Item1);
			}
		}
		else
		{
			Persistence.PassengerModeStatus = null;
		}
		if (_manualStopDistance.HasValue)
		{
			float value = _manualStopDistance.Value;
			float num8 = value / 12.192f;
			string reason;
			if (_orders.Mode == AutoEngineerMode.Waypoint)
			{
				reason = ((value > 1f) ? "Running to waypoint" : "At waypoint");
			}
			else
			{
				string text = ((num8 > 1f) ? ((!(num8 >= 20f)) ? ("Clear " + Mathf.FloorToInt(num8).Pluralize("car")) : "Clear 20+ cars") : ((!(num8 < 0.1f)) ? "Clear less than a car" : "That'll do!"));
				reason = text;
			}
			AddTarget(0f, value, reason);
		}
		List<TargetInfo> list = targetInfos.OrderBy((TargetInfo ti) => ti.Target.Distance).ToList();
		List<AutoEngineer.Targets.Target> list2 = list.Select((TargetInfo t) => t.Target).ToList();
		foreach (TargetInfo item3 in list)
		{
			if (!(Mathf.Abs(item3.Target.SpeedMph) > 0.001f))
			{
				if (item3.Target.Distance < num3)
				{
					stopAnnounce = item3.StopAnnounce;
				}
				break;
			}
		}
		_engineer.SetTargets(new AutoEngineer.Targets(direction * num5, list2, averageGradeUnder, averageGrade, changeDirection: false, _orders.Mode, stopAnnounce, found?.Item));
		SetPlannerStatus(num5, maxSpeedReason, list2);
		Persistence.ContextualOrders = contextualOrders;
		float num9 = availableDistance;
		foreach (AutoEngineer.Targets.Target item4 in list2)
		{
			if (Mathf.Abs(item4.SpeedMph) < 0.1f && item4.Distance < availableDistance)
			{
				num9 = item4.Distance;
			}
		}
		if (nextCrossingDistance.HasValue && nextCrossingDistance.Value > num9)
		{
			nextCrossingDistance = null;
		}
		_crossingSignaler.SetNextCrossingDistance(nextCrossingDistance);
		_log.Debug("ld={lookaheadDistance} -> ad={availableDistance}, max={maxSpeedMph}mph, {nextSignal} -> targets = {targets}", num, availableDistance, num5, found?.Item.name ?? "<no signal>", list2);
		void AddContextualOrder(ContextualOrder.OrderValue orderValue, string context)
		{
			contextualOrders.Add(new ContextualOrder(orderValue, context));
		}
		void AddTarget(float num10, float distance2, string reason2, StopAnnounce? maybeStopAnnounce = null)
		{
			TargetInfo item2 = new TargetInfo
			{
				Target = new AutoEngineer.Targets.Target(direction * num10, distance2, reason2),
				StopAnnounce = maybeStopAnnounce
			};
			targetInfos.Add(item2);
		}
	}

	private void CallSignalIfNeeded(CTCSignal signal, SignalAspect aspect)
	{
		string id = signal.id;
		if (_calledSignal?.signalId != id)
		{
			_calledSignal = null;
		}
		if (_calledSignal.HasValue)
		{
			(string, SignalAspect) value = _calledSignal.Value;
			if (value.Item1 == id && value.Item2 == aspect)
			{
				return;
			}
		}
		else if (aspect == SignalAspect.Clear || aspect == SignalAspect.DivergingClear)
		{
			_calledSignal = null;
			return;
		}
		_calledSignal = (id, aspect);
		CTCInterlocking interlocking = signal.Interlocking;
		if (!(interlocking == null))
		{
			string text = (string.IsNullOrEmpty(interlocking.displayName) ? interlocking.name : interlocking.displayName);
			Hyperlink hyperlink = Hyperlink.To(signal.transform, text);
			Say(string.Format("{0}, {1}.", aspect switch
			{
				SignalAspect.Stop => "Stop", 
				SignalAspect.Approach => "Approach", 
				SignalAspect.Clear => "Clear", 
				SignalAspect.DivergingApproach => "Diverging approach", 
				SignalAspect.DivergingClear => "Diverging clear", 
				SignalAspect.Restricting => "Restricting", 
				_ => throw new ArgumentOutOfRangeException("aspect", aspect, null), 
			}, hyperlink));
		}
	}

	private void SetSignalSpeedRestriction(int? speedAtSignalMph, int? approachSpeedMph, float distanceToSignal, CTCSignal nextSignal, bool distanceLimited = false)
	{
		float distanceLimit = (distanceLimited ? (distanceToSignal + BaseDistanceLimit) : float.PositiveInfinity);
		_lastSignalSpeedRestriction = ((!speedAtSignalMph.HasValue) ? ((SignalSpeedRestriction?)null) : new SignalSpeedRestriction?(new SignalSpeedRestriction(speedAtSignalMph.Value, approachSpeedMph, distanceToSignal, nextSignal.id, distanceLimit)));
	}

	private void SetPlannerStatus(float maxSpeedMphAbs, string maxSpeedReason, List<AutoEngineer.Targets.Target> targets = null)
	{
		float num = float.PositiveInfinity;
		if (targets != null)
		{
			foreach (AutoEngineer.Targets.Target target in targets)
			{
				float num2 = Mathf.Abs(target.SpeedMph);
				if (num2 < maxSpeedMphAbs && target.Distance < num)
				{
					maxSpeedMphAbs = num2;
					maxSpeedReason = target.Reason;
					num = target.Distance;
				}
			}
		}
		string plannerStatus = (AutoEngineer.IsZero(maxSpeedMphAbs) ? maxSpeedReason : $"{maxSpeedReason}: {maxSpeedMphAbs:F0}mph");
		if (_engineer.WaitingForBrakes)
		{
			plannerStatus = "Charging brake line";
		}
		_persistence.PlannerStatus = plannerStatus;
	}

	private void OffDuty()
	{
		_engineer.Run = false;
		_engineer.SetTargets(new AutoEngineer.Targets());
		_contextualIgnoreSignalId = null;
		_contextualIgnoreFlareId = null;
		_calledSignal = null;
		PostPitfallNotice(null);
	}

	private Location StartLocation(bool? overrideForward = null)
	{
		bool flag = overrideForward ?? _orders.Forward;
		Car.LogicalEnd num = _locomotive.EndToLogical((!flag) ? Car.End.R : Car.End.F);
		Car car = _coupledCarsCached[0];
		if (num == Car.LogicalEnd.A)
		{
			Location locationA = car.LocationA;
			if (!locationA.IsValid)
			{
				return car.WheelBoundsA;
			}
			return locationA;
		}
		Location locationB = car.LocationB;
		return (locationB.IsValid ? locationB : car.WheelBoundsB).Flipped();
	}

	private void DebugDrawLocation(Location location, float distance, Color color)
	{
	}

	private float? DistanceToNextSignalAfter(Found<CTCSignal> referenceSignalInfo, float availableDistance)
	{
		Graph graph = TrainController.Shared.graph;
		Location location = referenceSignalInfo.Location;
		float? result = null;
		foreach (TrackMarker item in graph.EnumerateTrackMarkers(location, availableDistance, sameDirection: true))
		{
			CTCSignal signal = item.Signal;
			if (signal != null && signal.isActiveAndEnabled && signal != referenceSignalInfo.Item)
			{
				result = graph.GetDistanceBetweenClose(location, item.Location.Value);
				return result;
			}
		}
		return result;
	}

	private void UpdateCars(out int delta)
	{
		int count = _coupledCarsCached.Count;
		Car.End fromEnd = (_coupledCarsCachedEnd = ((!_orders.Forward) ? Car.End.R : Car.End.F));
		_coupledCarsCached.Clear();
		_coupledCarsCached.AddRange(_locomotive.EnumerateCoupled(fromEnd));
		_maximumLength = CalculateTotalLength();
		delta = ((count != 0) ? (_coupledCarsCached.Count - count) : 0);
		_equipmentMaximumTrackCurvature = _coupledCarsCached.Min((Car car) => car.MaximumTrackCurvature);
		bool derailed = _derailed;
		_derailed = _coupledCarsCached.Any((Car car) => car.IsDerailed);
		if (_derailed && !derailed)
		{
			Say("We're on the ground!");
		}
		bool flag = _coupledCarsCached.Any((Car car) => car.IsPassengerCar()) && !IsYardMode;
		bool flag2 = _passengerStopper != null;
		if (flag != flag2)
		{
			if (flag)
			{
				_passengerStopper = base.gameObject.AddComponent<AutoEngineerPassengerStopper>();
			}
			else
			{
				UnityEngine.Object.Destroy(_passengerStopper);
				_passengerStopper = null;
			}
		}
		if (_passengerStopper != null)
		{
			_passengerStopper.UpdateCars(_coupledCarsCached);
			UpdateTimetableTrain();
		}
		if (_hotboxSpotter != null)
		{
			_hotboxSpotter.UpdateCars(_coupledCarsCached);
		}
	}

	private void UpdateTimetableTrain()
	{
		if (!(_passengerStopper == null))
		{
			Timetable.Train timetableTrain;
			bool flag = _timetableController.TryGetTrainForTrainCrewId(_locomotive.trainCrewId, out timetableTrain);
			_passengerStopper.SetTimetableTrain(flag ? timetableTrain : null);
		}
	}

	private float CalculateTotalLength()
	{
		List<Car> coupledCarsCached = _coupledCarsCached;
		float num = 0f;
		foreach (Car item in coupledCarsCached)
		{
			num += item.carLength;
		}
		return num + 1.04f * (float)(coupledCarsCached.Count - 1);
	}

	private bool ShouldStopForPitfall(out string pitfallStopReason)
	{
		if (IsYardMode && _manualStopDistance <= 0f)
		{
			pitfallStopReason = null;
			return false;
		}
		if (_engineer.HandbrakeApplied(out var numHandbrakes))
		{
			pitfallStopReason = numHandbrakes.Pluralize("handbrake") + " applied";
			return true;
		}
		if (!IsYardMode && !_engineer.BrakeLineTogether())
		{
			pitfallStopReason = "Check the brake line";
			return true;
		}
		if (!_engineer.BrakesReleasedOnNonAirConnectedCars())
		{
			pitfallStopReason = "Brake applied beyond brake line";
			return true;
		}
		pitfallStopReason = null;
		return false;
	}

	private string GetOverrideName()
	{
		if (string.IsNullOrEmpty(_locomotive.trainCrewId))
		{
			return null;
		}
		StateManager shared = StateManager.Shared;
		if (shared == null)
		{
			return null;
		}
		PlayersManager playersManager = shared.PlayersManager;
		if (playersManager == null)
		{
			return null;
		}
		if (!playersManager.TrainCrewForId(_locomotive.trainCrewId, out var trainCrew))
		{
			return null;
		}
		if (TimetableController.Shared.TryGetTrainForTrainCrew(trainCrew, out var timetableTrain))
		{
			return "Train " + timetableTrain.DisplayStringShort + ", " + _locomotive.DisplayName;
		}
		return _locomotive.DisplayName + ", " + trainCrew.Name;
	}

	public void Say(string message)
	{
		string overrideName = GetOverrideName();
		Multiplayer.Broadcast($"Auto Engineer {Hyperlink.To(_locomotive, overrideName)}: \"{message}\"");
	}

	public void HandleCommand(AutoEngineerCommand command, IPlayer sender)
	{
		Orders orders = _persistence.Orders;
		if (command.Mode == AutoEngineerMode.Waypoint)
		{
			bool flag = orders.Waypoint?.LocationString != command.WaypointLocationString;
			_routeRequester = (flag ? sender : null);
		}
		else
		{
			_routeRequester = null;
		}
		_persistence.Orders = new Orders(command.Mode, command.Forward, command.MaxSpeedMph, string.IsNullOrEmpty(command.WaypointLocationString) ? ((OrderWaypoint?)null) : new OrderWaypoint?(new OrderWaypoint(command.WaypointLocationString, command.WaypointCoupleToCarId)));
		_lastSignalSpeedRestriction = null;
		switch (command.Mode)
		{
		case AutoEngineerMode.Off:
		case AutoEngineerMode.Road:
			CancelManualStopDistance();
			break;
		case AutoEngineerMode.Yard:
			if (command.Distance.HasValue)
			{
				SetManualStopDistance(command.Distance.Value);
			}
			else if (orders.Mode != AutoEngineerMode.Yard)
			{
				SetManualStopDistance(0f);
			}
			break;
		default:
			throw new ArgumentOutOfRangeException();
		case AutoEngineerMode.Waypoint:
			break;
		}
	}

	public void HandleRequestReroute(IPlayer sender)
	{
		_routeRequester = sender;
		UpdateWaypointRouteIfNeeded(force: true);
	}

	public void HandleContextualOrder(AutoEngineerContextualOrder contextualOrder)
	{
		switch (contextualOrder.Order)
		{
		case ContextualOrder.OrderValue.PassSignal:
			_contextualIgnoreSignalId = contextualOrder.Context;
			if (_lastSignalSpeedRestriction.HasValue && _lastSignalSpeedRestriction.Value.SignalId == _contextualIgnoreSignalId)
			{
				SignalSpeedRestriction value = _lastSignalSpeedRestriction.Value;
				value.SpeedMph = Mathf.Max(value.SpeedMph, 15);
				value.DistanceLimit = value.DistanceToSignal + BaseDistanceLimit;
				_lastSignalSpeedRestriction = value;
			}
			break;
		case ContextualOrder.OrderValue.PassFlare:
			_contextualIgnoreFlareId = contextualOrder.Context;
			break;
		case ContextualOrder.OrderValue.ResumeSpeed:
			_contextualIgnoreSignalId = contextualOrder.Context;
			_lastSignalSpeedRestriction = null;
			break;
		case ContextualOrder.OrderValue.BypassTimetable:
			_contextualBypassTimetableStation = contextualOrder.Context;
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	private void SetManualStopDistance(float distanceInMeters)
	{
		_manualStopDistance = distanceInMeters;
	}

	private void CancelManualStopDistance()
	{
		_manualStopDistance = null;
	}

	public void ApplyMovement(MovementInfo info)
	{
		_engineer.ApplyMovement(info);
		if (_manualStopDistance.HasValue)
		{
			_manualStopDistance -= info.Distance;
		}
		foreach (AutoEngineerComponentBase item in Components())
		{
			item.ApplyMovement(info);
		}
		if (_lastSignalSpeedRestriction.HasValue)
		{
			SignalSpeedRestriction value = _lastSignalSpeedRestriction.Value;
			value.DistanceToSignal -= info.Distance;
			value.DistanceLimit -= info.Distance;
			_lastSignalSpeedRestriction = ((value.DistanceLimit < 0f) ? ((SignalSpeedRestriction?)null) : new SignalSpeedRestriction?(value));
			if (!_lastSignalSpeedRestriction.HasValue)
			{
				_log.Information("Cleared signal speed restriction.");
			}
		}
		_underTrainPoints.Clear();
	}

	public void WillMove()
	{
		_lastSignalSpeedRestriction = null;
		_contextualIgnoreFlareId = null;
		_contextualIgnoreSignalId = null;
		_calledSignal = null;
		_stopAndProceedSignalId = null;
		foreach (AutoEngineerComponentBase item in Components())
		{
			item.WillMove();
		}
	}

	private IEnumerable<AutoEngineerComponentBase> Components()
	{
		if (_passengerStopper != null)
		{
			yield return _passengerStopper;
		}
		if (_crossingSignaler != null)
		{
			yield return _crossingSignaler;
		}
	}

	private void PostPitfallNotice(string message)
	{
		_locomotive.PostNotice("ai-pitfall", message);
	}

	private bool OrdersWantMovement()
	{
		return _orders.Mode switch
		{
			AutoEngineerMode.Off => false, 
			AutoEngineerMode.Road => _orders.MaxSpeedMph > 0, 
			AutoEngineerMode.Yard => _manualStopDistance > 0f, 
			AutoEngineerMode.Waypoint => _orders.Waypoint.HasValue, 
			_ => false, 
		};
	}

	private IEnumerator RouteLoop()
	{
		while (_orders.Mode == AutoEngineerMode.Waypoint && _orders.Waypoint.HasValue)
		{
			yield return TickRouteIfNeeded();
			yield return AutoEngineer.WaitFixed(1f);
		}
		_routeCoroutine = null;
	}

	private IEnumerator TickRouteIfNeeded()
	{
		yield return null;
		try
		{
			TickRoute();
		}
		catch (Exception exception)
		{
			_log.Error(exception, "Exception in TickRoute");
		}
	}

	private Location RouteStartLocation(out float trainMomentum)
	{
		bool num = _locomotive.IsStopped();
		bool? flag = (num ? ((bool?)null) : new bool?(_locomotive.velocity >= 0f));
		if (num)
		{
			trainMomentum = 0f;
		}
		else
		{
			float trainMass = _coupledCarsCached.Sum((Car c) => c.Weight * 0.4536f);
			float totalAvailableBraking = _engineer.CalculateTotalAvailableBraking();
			float num2 = AutoEngineer.CalculateDistanceToSlowToSpeed(Mathf.Abs(_locomotive.velocity), 0f, totalAvailableBraking, trainMass);
			trainMomentum = _config.momentumFactor * num2 + _config.momentumOffset;
		}
		bool flag2 = flag ?? _orders.Forward;
		if (_locomotive.EndToLogical((!flag2) ? Car.End.R : Car.End.F) == Car.LogicalEnd.A)
		{
			return _locomotive.EnumerateCoupled().First().WheelBoundsA;
		}
		return _locomotive.EnumerateCoupled(Car.LogicalEnd.B).First().WheelBoundsB.Flipped();
	}

	private void TickRoute()
	{
		Vector3 positionF;
		Vector3 positionR;
		if (_orders.Mode == AutoEngineerMode.Waypoint)
		{
			OrderWaypoint? waypoint = _orders.Waypoint;
			if (waypoint.HasValue)
			{
				OrderWaypoint valueOrDefault = waypoint.GetValueOrDefault();
				UpdateWaypointRouteIfNeeded();
				if (_route == null || _route.Count == 0)
				{
					return;
				}
				if (IsWaypointSatisfied(valueOrDefault))
				{
					RestoreAllRemainingSwitches();
					ClearRoute();
					PostWaypointNotice("Arrived at waypoint!");
					Orders orders = _persistence.Orders;
					_manualStopDistance = 0f;
					_persistence.Orders = new Orders(orders.Mode, orders.Forward, orders.MaxSpeedMph, null);
					return;
				}
				GetEndLocationsFrontRear(out var locationF, out var locationR);
				positionF = _graph.GetPosition(locationF);
				positionR = _graph.GetPosition(locationR);
				UpdateStartStepIndex();
				RerouteIfNotOnCurrentRoute(locationF, locationR);
				if (_route.Count < _startStepIndex + 2)
				{
					return;
				}
				HashSet<string> hashSet = new HashSet<string>();
				float sumMovement = 0f;
				bool flag = false;
				for (int i = _startStepIndex + 1; i < _route.Count; i++)
				{
					RouteSearch.Step step = _route[i];
					bool flag2 = false;
					if (TryGetDesiredSwitchSetting(i, out var switchNode, out var thrown))
					{
						if (switchNode.IsCTCSwitch)
						{
							flag = true;
						}
						else if (flag && switchNode.isThrown != thrown)
						{
							flag2 = true;
						}
						if (switchNode.isThrown != thrown && !flag2)
						{
							if (!CanSetSwitchAtStep(i, locationF, locationR))
							{
								flag2 = true;
							}
							else if (!hashSet.Add(switchNode.id))
							{
								flag2 = true;
							}
							else
							{
								Vector3 position = step.Position;
								if (Mathf.Min(Vector3.Distance(positionF, position), Vector3.Distance(positionR, position)) <= 150f)
								{
									SetSwitchResult setSwitchResult = TrySetSwitch(switchNode, thrown);
									Log.Debug("TrySetSwitch {node} to thrown = {to} -> {result}", switchNode, thrown, setSwitchResult);
									switch (setSwitchResult)
									{
									case SetSwitchResult.Occupied:
										flag2 = true;
										break;
									default:
										throw new ArgumentOutOfRangeException();
									case SetSwitchResult.CTC:
									case SetSwitchResult.Success:
										break;
									}
								}
							}
						}
					}
					AddDistanceTo(step, ref sumMovement, i == _startStepIndex + 1);
					if (flag2)
					{
						break;
					}
					if (i < _route.Count - 1)
					{
						for (int j = i + 1; j < _route.Count; j++)
						{
							if (_route[j].Direction != step.Direction)
							{
								int num = j - 1;
								RouteSearch.Step step2 = _route[num];
								if (Vector3.Distance(step2.Position, positionF) > 150f && Vector3.Distance(step2.Position, positionR) > 150f)
								{
									break;
								}
								CheckForFoulingPointLimitingSwitch(step2.Location, num);
							}
						}
						if (_route[i + 1].Direction != step.Direction)
						{
							sumMovement += 3f;
							break;
						}
					}
					if (sumMovement > 1609.344f)
					{
						break;
					}
				}
				if (valueOrDefault.WantsCouple)
				{
					sumMovement += 3f;
				}
				RestorePassedSwitchesToOriginalPosition();
				Vector3 position2 = _route[_startStepIndex + 1].Position;
				if (Vector3.Distance(positionF, position2) > Vector3.Distance(positionR, position2))
				{
					sumMovement *= -1f;
				}
				Log.Debug("RouteAI Movement: {meters}", sumMovement);
				SetDirection(sumMovement >= 0f);
				_manualStopDistance = Mathf.Abs(sumMovement);
				return;
			}
		}
		ClearRoute();
		void AddDistanceTo(RouteSearch.Step step3, ref float reference, bool isInitialStep)
		{
			if (isInitialStep)
			{
				Vector3 position3 = step3.Position;
				reference += Mathf.Min(Vector3.Distance(position3, positionF), Vector3.Distance(position3, positionR));
			}
			else
			{
				reference += step3.Distance;
			}
		}
	}

	private bool CanSetSwitchAtStep(int stepIndex, Location locationF, Location locationR)
	{
		if (_orders.MaxSpeedMph == 0)
		{
			return false;
		}
		if (_passengerStopper != null && _passengerStopper.IsStoppedAtStation)
		{
			return false;
		}
		RouteSearch.Step step = _route[stepIndex];
		Location start = _graph.ClosestLocationFacing(locationF, locationR, step.Location);
		return !IsSwitchBlockerPresent(start, step.Location, step.Node);
	}

	private bool IsSwitchBlockerPresent(Location start, Location end, TrackNode switchNode)
	{
		float distanceBetweenClose = _graph.GetDistanceBetweenClose(start, end);
		foreach (TrackMarker item in _graph.EnumerateTrackMarkers(start, distanceBetweenClose, sameDirection: false))
		{
			if (item.type == TrackMarkerType.Flare && FlareManager.TryGetFlarePickable(item, out var _))
			{
				return true;
			}
		}
		bool isCTC = IsCTC;
		foreach (TrackMarker item2 in _graph.EnumerateTrackMarkers(start, distanceBetweenClose, sameDirection: true))
		{
			if (item2.type != TrackMarkerType.Signal)
			{
				continue;
			}
			CTCSignal signal = item2.Signal;
			if (!signal.isActiveAndEnabled)
			{
				continue;
			}
			CTCInterlocking interlocking = signal.Interlocking;
			if (interlocking == null || isCTC)
			{
				continue;
			}
			bool flag = false;
			foreach (CTCInterlocking.SwitchSet switchSet in interlocking.switchSets)
			{
				foreach (TrackNode switchNode2 in switchSet.switchNodes)
				{
					if (switchNode == switchNode2)
					{
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				return true;
			}
		}
		return false;
	}

	private bool IsWaypointSatisfied(OrderWaypoint waypoint)
	{
		if (waypoint.WantsCouple)
		{
			bool flag = false;
			foreach (Car item in _coupledCarsCached)
			{
				if (item.id == waypoint.CoupleToCarId)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		if (!IsUnderTrain(_routeTargetLocation.Value, 0f))
		{
			return false;
		}
		return true;
	}

	private void RerouteIfNotOnCurrentRoute(Location locationA, Location locationB)
	{
		if (!IsOnCurrentRoute(locationA, locationB))
		{
			float unscaledTime = Time.unscaledTime;
			float num = unscaledTime - _lastNotCurrentRouteReroute;
			if (num < 10f)
			{
				_log.Warning("Not on current route but too soon to reroute. {interval}", num);
				return;
			}
			_log.Warning("Not on current route, rebuilding. {interval}", num);
			_lastNotCurrentRouteReroute = unscaledTime;
			UpdateWaypointRouteIfNeeded(force: true);
		}
	}

	private bool IsOnCurrentRoute(Location locationA, Location locationB)
	{
		if (_route == null || _startStepIndex + 1 >= _route.Count)
		{
			return false;
		}
		RouteSearch.Step step = _route[_startStepIndex + 1];
		Location location = step.Location;
		Vector3 position = step.Position;
		float num = Vector3.Distance(position, _graph.GetPosition(locationA));
		float num2 = Vector3.Distance(position, _graph.GetPosition(locationB));
		if (num < num2)
		{
			return _graph.CheckSameRoute(locationA, location, num * 2f);
		}
		return _graph.CheckSameRoute(locationB, location, num2 * 2f);
	}

	private void UpdateWaypointRouteIfNeeded(bool force = false, float baseMomentum = 0f)
	{
		if (_orders.Mode != AutoEngineerMode.Waypoint)
		{
			ClearRoute();
			return;
		}
		OrderWaypoint? waypoint = _orders.Waypoint;
		if (waypoint.HasValue)
		{
			OrderWaypoint valueOrDefault = waypoint.GetValueOrDefault();
			if (_coupledCarsCached.Count == 0 || (!force && valueOrDefault.Equals(_lastRouteWaypoint)))
			{
				return;
			}
			_lastRouteWaypoint = valueOrDefault;
			try
			{
				_log.Debug("Updating route...");
				RebuildRoute(valueOrDefault, baseMomentum);
				List<RouteSearch.Step> route = _route;
				if (route != null && route.Count > 0)
				{
					SetInitialDirection();
				}
				return;
			}
			catch (Exception exception)
			{
				_log.Error(exception, "Error updating route");
				return;
			}
			finally
			{
				FireChangeMessage(routeChanged: true);
			}
		}
		if (_route != null)
		{
			ClearRoute();
		}
		_manualStopDistance = 0f;
	}

	private void SetInitialDirection()
	{
		GetEndLocations(out var locationA, out var locationB);
		Vector3 position = _route[_startStepIndex].Position;
		bool flag = Vector3.Distance(_graph.GetPosition(locationA), position) < Vector3.Distance(_graph.GetPosition(locationB), position);
		SetDirection(_locomotive.FrontIsA == flag);
	}

	private bool RebuildRoute(OrderWaypoint waypoint, float baseMomentum)
	{
		Graph graph = _graph;
		Location location;
		try
		{
			location = graph.ResolveLocationString(waypoint.LocationString);
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Couldn't get location from waypoint: {locStr}", waypoint.LocationString);
			ClearRoute();
			return false;
		}
		float trainMomentum;
		Location start = RouteStartLocation(out trainMomentum);
		Location end = location;
		_routeTargetLocation = location;
		_debugLastSearchCosts = null;
		trainMomentum += baseMomentum;
		HeuristicCosts autoEngineer = HeuristicCosts.AutoEngineer;
		List<RouteSearch.Step> list = new List<RouteSearch.Step>();
		if (!graph.FindRoute(start, end, autoEngineer, list, out var metrics, checkForCars: false, _maximumLength, trainMomentum))
		{
			RouteSearch.Metrics metrics2;
			bool flag = graph.FindRoute(start, end, autoEngineer, null, out metrics2);
			SendMessageToRouteRequester(flag ? (_locomotive.DisplayName + " Train too long to navigate to waypoint.") : (_locomotive.DisplayName + " Unable to find a path to waypoint."), AlertLevel.Error);
			_manualStopDistance = 0f;
			return false;
		}
		HashSet<string> hashSet = new HashSet<string>();
		foreach (RouteSearch.Step item in list)
		{
			if (item.HasFlag(RouteSearch.StepFlag.EnterCTCSwitch))
			{
				hashSet.Add(item.Node.id);
			}
		}
		HashSet<Car> hashSet2 = new HashSet<Car>();
		if (waypoint.WantsCouple)
		{
			Car car = TrainController.Shared.CheckForCarAtLocation(location);
			if (car != null && car.id == waypoint.CoupleToCarId)
			{
				hashSet2.Add(car);
			}
			else
			{
				Log.Warning("Couldn't find 'Couple' car at location. Expected {carId}, found {found}", waypoint.CoupleToCarId, car);
			}
		}
		_route = new List<RouteSearch.Step>();
		PostWaypointNotice(null);
		RouteSearch.Metrics metrics3;
		bool num = graph.FindRoute(start, end, autoEngineer, _route, out metrics3, checkForCars: true, _maximumLength, trainMomentum, 5000, _coupledCarsCached.ToHashSet(), hashSet2, hashSet, enableLogging: true);
		_startStepIndex = 0;
		if (!num)
		{
			SendMessageToRouteRequester(_locomotive.DisplayName + " Route to waypoint is blocked.", AlertLevel.Error);
			_manualStopDistance = 0f;
			return false;
		}
		float distance = metrics.Distance;
		SendMessageToRouteRequester(_locomotive.DisplayName + " waypoint set: " + Units.DistanceText(distance), AlertLevel.Info);
		return true;
	}

	private void SendMessageToRouteRequester(string message, AlertLevel alertLevel)
	{
		if (_routeRequester == null)
		{
			Log.Warning("RouteAI: Message can't be delivered, no requester: {message}", message);
			return;
		}
		Multiplayer.SendError(_routeRequester, message, alertLevel);
		_routeRequester = null;
	}

	private void CheckForFoulingPointLimitingSwitch(Location turnBackLocation, int futureStepIndex)
	{
		Graph shared = Graph.Shared;
		try
		{
			shared.LocationByMoving(turnBackLocation, 75f, checkSwitchAgainstMovement: true, stopAtEndOfTrack: true);
		}
		catch (SwitchAgainstMovement switchAgainstMovement)
		{
			TrackNode node = switchAgainstMovement.Node;
			float num = Vector3.Distance(node.transform.GamePosition(), shared.GetPosition(turnBackLocation));
			if (!(shared.CalculateFoulingDistance(node) + 10f < num))
			{
				bool flag = !node.isThrown;
				if (node.IsCTCSwitch && !node.IsCTCSwitchUnlocked)
				{
					Log.Warning("RouteAI: Switch {node} is fouling but can't throw CTC switch", node.id);
					return;
				}
				if (!TrainController.Shared.CanSetSwitch(node, flag, out var foundCar))
				{
					Log.Warning("RouteAI: Switch {node} is fouling but can't set switch: {car}", node.id, foundCar);
					return;
				}
				Log.Debug("RouteAI: Switch {node} is fouling; flipping", node.id);
				_routeExtraSwitches.Add(futureStepIndex, node.id);
				TrySetSwitch(node, flag);
			}
		}
		catch (Exception)
		{
		}
	}

	private void RestorePassedSwitchesToOriginalPosition()
	{
		Graph shared = Graph.Shared;
		HashSet<TrackNode> value;
		using (CollectionPool<HashSet<TrackNode>, TrackNode>.Get(out value))
		{
			foreach (KeyValuePair<string, bool> item in _routeSwitchesToRestore)
			{
				item.Deconstruct(out var key, out var value2);
				string id = key;
				bool thrown = value2;
				TrackNode node = shared.GetNode(id);
				if (!IsInFutureStep(node) && TrainController.Shared.CanSetSwitch(node, thrown, out var _))
				{
					value.Add(node);
				}
			}
			foreach (TrackNode item2 in value)
			{
				if (_routeSwitchesToRestore.TryGetValue(item2.id, out var value3))
				{
					SetSwitchThrown(item2, value3);
				}
				_routeSwitchesToRestore.Remove(item2.id);
			}
		}
		bool IsInFutureStep(TrackNode trackNode)
		{
			for (int i = _startStepIndex; i < _route.Count; i++)
			{
				if (_route[i].Node == trackNode)
				{
					return true;
				}
				if (_routeExtraSwitches.Contains(i, trackNode.id))
				{
					return true;
				}
			}
			return false;
		}
	}

	private void RestoreAllRemainingSwitches()
	{
		TrainController shared = TrainController.Shared;
		foreach (KeyValuePair<string, bool> item in _routeSwitchesToRestore)
		{
			item.Deconstruct(out var key, out var value);
			string id = key;
			bool flag = value;
			TrackNode node = _graph.GetNode(id);
			if (shared.CanSetSwitch(node, flag, out var _))
			{
				SetSwitchThrown(node, flag);
			}
		}
		_routeSwitchesToRestore.Clear();
	}

	private SetSwitchResult TrySetSwitch(TrackNode node, bool isThrown)
	{
		if (!TrainController.Shared.CanSetSwitch(node, isThrown, out var _))
		{
			return SetSwitchResult.Occupied;
		}
		if (node.IsCTCSwitch && !node.IsCTCSwitchUnlocked)
		{
			return SetSwitchResult.CTC;
		}
		Log.Debug("RouteAI: Setting switch {nodeId} {thrown}", node.id, isThrown ? "N" : "R");
		bool isThrown2 = node.isThrown;
		SetSwitchThrown(node, isThrown);
		_routeSwitchesToRestore.TryAdd(node.id, isThrown2);
		return SetSwitchResult.Success;
	}

	private void SetSwitchThrown(TrackNode node, bool isThrown)
	{
		if (!TrainController.Shared.TrySetSwitch(node.id, isThrown, "AE " + _locomotive.DisplayName, out var errorMessage))
		{
			Log.Error("Error from TrySetSwitch: {error}", errorMessage);
		}
	}

	private bool TryGetDesiredSwitchSetting(int stepIndex, out TrackNode switchNode, out bool thrown)
	{
		if (stepIndex < 0 || stepIndex >= _route.Count)
		{
			switchNode = null;
			thrown = false;
			return false;
		}
		switchNode = _route[stepIndex].Node;
		thrown = false;
		if (switchNode == null)
		{
			return false;
		}
		if (!_graph.DecodeSwitchAt(switchNode, out var enter, out var a, out var b))
		{
			return false;
		}
		bool flag;
		if (NextStepMatchesSegment(enter))
		{
			RouteSearch.Step step = _route[stepIndex - 1];
			if (step.Node != null)
			{
				if (a.Contains(step.Node))
				{
					flag = false;
				}
				else
				{
					if (!b.Contains(step.Node))
					{
						return false;
					}
					flag = true;
				}
			}
			else
			{
				TrackSegment segment = step.Location.segment;
				if (a == segment)
				{
					flag = false;
				}
				else
				{
					if (!(b == segment))
					{
						return false;
					}
					flag = true;
				}
			}
		}
		else if (NextStepMatchesSegment(a))
		{
			flag = false;
		}
		else
		{
			if (!NextStepMatchesSegment(b))
			{
				return false;
			}
			flag = true;
		}
		thrown = flag;
		return true;
		bool NextStepMatchesSegment(TrackSegment trackSegment)
		{
			if (stepIndex >= _route.Count - 1)
			{
				return false;
			}
			RouteSearch.Step step2 = _route[stepIndex + 1];
			if (step2.Node != null)
			{
				return trackSegment.Contains(step2.Node);
			}
			return step2.Location.segment == trackSegment;
		}
	}

	private void UpdateStartStepIndex()
	{
		bool flag = false;
		int startStepIndex = _startStepIndex;
		int num = Mathf.Min(_startStepIndex + 3, _route.Count - 1);
		GetEndLocations(out var locationA, out var locationB);
		Location loc = ((_locomotive.FrontIsA == _orders.Forward) ? locationA : locationB.Flipped());
		Graph.PositionDirection positionDirection = _graph.GetPositionDirection(loc);
		for (int i = _startStepIndex; i < num; i++)
		{
			RouteSearch.Step step = _route[i];
			if (i >= _startStepIndex + 1 && SwitchNeedsToBeLined(i))
			{
				break;
			}
			Vector3 position = step.Position;
			float num2 = Vector3.Dot(positionDirection.Direction, (position - positionDirection.Position).normalized);
			bool flag2 = num2 > 0.5f;
			bool flag3 = num2 < -0.5f;
			if (flag2)
			{
				break;
			}
			if (flag3)
			{
				_startStepIndex = i;
				flag = true;
			}
			else if (flag)
			{
				break;
			}
		}
		if (startStepIndex == _startStepIndex)
		{
			return;
		}
		for (int j = startStepIndex; j <= _startStepIndex; j++)
		{
			RouteSearch.Step step2 = _route[j];
			if (step2.HasFlag(RouteSearch.StepFlag.SearchLimit))
			{
				if (step2.HasFlag(RouteSearch.StepFlag.EnterCTCSwitch))
				{
					UpdateWaypointRouteIfNeeded(force: true, _config.momentumRerouteAtCtcSwitch);
				}
				else
				{
					UpdateWaypointRouteIfNeeded(force: true);
				}
				return;
			}
		}
		Log.Debug("Waypoint Overlay: startStepIndex change: {old} -> {new}", startStepIndex, _startStepIndex);
		FireChangeMessage(routeChanged: false);
	}

	private void FireChangeMessage(bool routeChanged)
	{
		Snapshot.TrackLocation? current = ((_route == null || _startStepIndex >= _route.Count) ? ((Snapshot.TrackLocation?)null) : new Snapshot.TrackLocation?(Graph.CreateSnapshotTrackLocation(_route[_startStepIndex].Location)));
		StateManager.ApplyLocal(new AutoEngineerWaypointRouteUpdate(_locomotive.id, current, routeChanged));
	}

	private bool SwitchNeedsToBeLined(int stepIndex)
	{
		if (!TryGetDesiredSwitchSetting(stepIndex, out var switchNode, out var thrown))
		{
			return false;
		}
		if (switchNode.IsCTCSwitch && !switchNode.IsCTCSwitchUnlocked)
		{
			return false;
		}
		return switchNode.isThrown != thrown;
	}

	private void SetDirection(bool forward)
	{
		_persistence.Orders = new Orders(_orders.Mode, forward, _orders.MaxSpeedMph, _orders.Waypoint);
	}

	private void ClearRoute()
	{
		_route = null;
		_routeTargetLocation = null;
		_lastRouteWaypoint = default(OrderWaypoint);
		_routeSwitchesToRestore.Clear();
		_routeExtraSwitches.Clear();
		PostWaypointNotice(null);
	}

	private void PostWaypointNotice(string message)
	{
		bool flag = string.IsNullOrEmpty(message);
		if (!flag || !_waypointNoticeCleared)
		{
			_locomotive.PostNotice("ai-wpt", message);
			_waypointNoticeCleared = flag;
		}
	}

	private void GetEndLocations(out Location locationA, out Location locationB)
	{
		bool flag = _coupledCarsCachedEnd == Car.End.F == _locomotive.FrontIsA;
		List<Car> coupledCarsCached = _coupledCarsCached;
		locationA = coupledCarsCached[(flag ? ((Index)0) : (^1)).GetOffset(coupledCarsCached.Count)].LocationA;
		coupledCarsCached = _coupledCarsCached;
		locationB = coupledCarsCached[(flag ? (^1) : ((Index)0)).GetOffset(coupledCarsCached.Count)].LocationB;
	}

	private void GetEndLocationsFrontRear(out Location locationF, out Location locationR)
	{
		bool flag = _coupledCarsCachedEnd == Car.End.F;
		List<Car> coupledCarsCached = _coupledCarsCached;
		locationF = coupledCarsCached[(flag ? ((Index)0) : (^1)).GetOffset(coupledCarsCached.Count)].LocationF;
		coupledCarsCached = _coupledCarsCached;
		locationR = coupledCarsCached[(flag ? (^1) : ((Index)0)).GetOffset(coupledCarsCached.Count)].LocationR;
	}

	private bool IsUnderTrain(Location location, float inset, bool headEndOnly = false)
	{
		GetEndLocations(out var locationA, out var locationB);
		Location location2 = _graph.LocationByMoving(locationA, 0f - inset, checkSwitchAgainstMovement: false, stopAtEndOfTrack: true);
		Location location3 = _graph.LocationByMoving(locationB, inset, checkSwitchAgainstMovement: false, stopAtEndOfTrack: true);
		if (_underTrainPoints.Count == 0)
		{
			_graph.FindPoints(location2, location3, 5f, "IsUnderTrain", _underTrainPoints);
		}
		if (_underTrainPoints.Count == 0)
		{
			Log.Error("Couldn't find points under train: {locA} {locB}", location2, location3);
			return false;
		}
		Vector3 position = _graph.GetPosition(location);
		bool result = false;
		int num = 0;
		int num2 = _underTrainPoints.Count;
		if (headEndOnly)
		{
			num2 = Mathf.Max(2, num2 / 2);
			if (_locomotive.EndToLogical((!_orders.Forward) ? Car.End.R : Car.End.F) == Car.LogicalEnd.B)
			{
				num2++;
				num = _underTrainPoints.Count - num2;
			}
		}
		num2 = Mathf.Clamp(num2, 2, _underTrainPoints.Count);
		for (int i = num; i < num + num2 - 1; i++)
		{
			Vector3 point = _underTrainPoints[i];
			Vector3 point2 = _underTrainPoints[i + 1];
			if (Vector3.SqrMagnitude(new LineSegment(new LinePoint(point, Quaternion.identity), new LinePoint(point2, Quaternion.identity)).ClosestPointTo(position) - position) < 1f)
			{
				result = true;
				break;
			}
		}
		return result;
	}

	private void DrawRouteGizmos()
	{
	}

	public void GetWaypointRouteLocation(List<Location> locations, out bool hasMoreSteps)
	{
		if (_route == null)
		{
			locations.Clear();
			hasMoreSteps = false;
			return;
		}
		int num = _route.Count - _startStepIndex;
		int num2 = Mathf.Min(10, num);
		hasMoreSteps = num > num2;
		locations.Clear();
		for (int i = 0; i < num2 - 1; i++)
		{
			RouteSearch.Step step = _route[_startStepIndex + i];
			locations.Add(step.Location);
			if (step.HasFlag(RouteSearch.StepFlag.SearchLimit))
			{
				break;
			}
		}
		Location? routeTargetLocation = _routeTargetLocation;
		if (routeTargetLocation.HasValue)
		{
			Location valueOrDefault = routeTargetLocation.GetValueOrDefault();
			locations.Add(valueOrDefault);
		}
	}

	private static void Search(Car headCar, float velocityAbs, Location start, float lookaheadDistance, SearchMode mode, float equipmentMaximumTrackCurvature, OtherCarHandling otherCarHandling, SwitchAgainstHandling switchAgainstHandling, ICollection<Car> coupledCars, out SearchResult result)
	{
		TrainController shared = TrainController.Shared;
		Graph graph = shared.graph;
		Location cursor = start;
		float num = lookaheadDistance;
		result.AvailableDistance = 0f;
		result.AvailableDistanceReason = "";
		result.MaxSpeedMph = MaxSpeedForTrackMph(cursor);
		result.MaxSpeedMphNear = result.MaxSpeedMph;
		result.NextSignal = null;
		result.NextFlare = null;
		result.NextCrossingDistance = null;
		result.NextPassengerStop = null;
		result.NextRestrictionDistance = lookaheadDistance;
		result.AverageGrade = 0f;
		result.DistanceLimiter = DistanceLimiter.Other;
		result.StopAnnounce = null;
		result.LimitingCarRelativeVelocity = 0f;
		float num2 = velocityAbs * 5f;
		Car car = null;
		float num3 = 0f;
		float limitingCarRelativeVelocity = 0f;
		bool flag = false;
		bool flag2 = false;
		while (num > 0f)
		{
			num -= 10f;
			try
			{
				Location location = cursor;
				cursor = graph.LocationByMoving(cursor, 10f, !flag2);
				flag2 = false;
				float currentAvailableDistance;
				if (mode == SearchMode.Ahead && car == null)
				{
					Car car2 = shared.CheckForCarAtLocation(cursor);
					currentAvailableDistance = result.AvailableDistance;
					if (car2 != null && ShouldConsiderCar(car2))
					{
						car = car2;
						limitingCarRelativeVelocity = shared.RelativeVelocity(headCar, car);
						float num4 = DistanceToCar(location, car2, graph);
						num3 = result.AvailableDistance + num4;
						OtherCarBehavior behavior = otherCarHandling.Behavior;
						if (behavior != OtherCarBehavior.Couple)
						{
							if ((uint)(behavior - 1) > 1u)
							{
								throw new ArgumentOutOfRangeException();
							}
							goto IL_021c;
						}
						if (!IsOnSameRoute(car))
						{
							num3 -= 10f;
							flag = true;
						}
						else
						{
							if (!string.IsNullOrEmpty(otherCarHandling.CarId) && !(otherCarHandling.CarId == car2.id))
							{
								goto IL_021c;
							}
							num3 += 2f;
						}
						goto IL_0272;
					}
				}
				goto IL_0282;
				IL_0272:
				if (num3 < 0f)
				{
					num3 = 0f;
				}
				goto IL_0282;
				IL_021c:
				if (!IsOnSameRoute(car))
				{
					num3 -= 10f;
					flag = true;
				}
				else
				{
					int num5 = ((otherCarHandling.Behavior != OtherCarBehavior.Avoid) ? 1 : 20);
					num3 = ((!shared.StoppingDistanceIfMovingToward(headCar, car, out var _, out var stoppingDistanceForA)) ? (num3 - (float)num5) : (stoppingDistanceForA - (float)num5));
				}
				goto IL_0272;
				IL_0282:
				result.AvailableDistance += 10f;
				float num6 = MaxSpeedForTrackMph(cursor);
				if (num6 < result.MaxSpeedMph)
				{
					result.MaxSpeedMph = num6;
					result.NextRestrictionDistance = Mathf.Min(result.NextRestrictionDistance, result.AvailableDistance);
				}
				if (result.AvailableDistance < num2 && num6 < result.MaxSpeedMphNear)
				{
					result.MaxSpeedMphNear = num6;
				}
				result.AverageGrade += graph.GradeAtLocation(cursor);
				bool IsOnSameRoute(Car other)
				{
					float limit = currentAvailableDistance + 10f + other.carLength;
					if (graph.CheckSameRoute(cursor, other.LocationF, limit))
					{
						return graph.CheckSameRoute(cursor, other.LocationR, limit);
					}
					return false;
				}
			}
			catch (SwitchAgainstMovement switchAgainstMovement)
			{
				StopAnnounce stopAnnounce = StopAnnounce.SwitchAgainst;
				if (switchAgainstHandling == SwitchAgainstHandling.FoulThrowableSwitches && CheckThrowable(switchAgainstMovement.Node, shared, coupledCars, out stopAnnounce))
				{
					flag2 = true;
					num += 10f;
					continue;
				}
				float num7 = graph.CalculateFoulingDistance(switchAgainstMovement.Node);
				float num8 = Vector3.Distance(graph.GetPosition(cursor), switchAgainstMovement.Node.transform.GamePosition());
				result.AvailableDistance = result.AvailableDistance + num8 - num7;
				result.AvailableDistanceReason = stopAnnounce switch
				{
					StopAnnounce.SwitchFouled => "Switch Fouled", 
					StopAnnounce.CTCSwitchLocked => "CTC Switch Locked", 
					_ => "Switch Against", 
				};
				result.StopAnnounce = stopAnnounce;
				break;
			}
			catch (EndOfTrack)
			{
				Location b = graph.LocationByMoving(cursor, 10f, checkSwitchAgainstMovement: true, stopAtEndOfTrack: true);
				result.AvailableDistance += graph.GetDistanceBetweenClose(cursor, b);
				result.AvailableDistanceReason = "End of Track";
				break;
			}
		}
		if (car != null && num3 < result.AvailableDistance)
		{
			result.AvailableDistance = num3;
			result.DistanceLimiter = DistanceLimiter.Car;
			result.StopAnnounce = (flag ? StopAnnounce.SwitchFouled : StopAnnounce.OtherTrain);
			result.LimitingCarRelativeVelocity = limitingCarRelativeVelocity;
			result.AvailableDistanceReason = (flag ? (car.DisplayName + " Fouling Switch") : ("Approaching " + car.DisplayName));
		}
		if (mode == SearchMode.Ahead)
		{
			foreach (TrackMarker item in graph.EnumerateTrackMarkers(start, result.AvailableDistance, sameDirection: true))
			{
				CTCSignal signal = item.Signal;
				if (signal != null && signal.isActiveAndEnabled)
				{
					Location value = item.Location.Value;
					float distance2;
					try
					{
						distance2 = graph.GetDistanceBetweenClose(start, value);
					}
					catch (Exception exception)
					{
						Log.Error(exception, "GetDistanceBetweenClose threw exception: {signal}, {start}, {nextSignalLoc} ({startIsValid}, {nextSignalLocIsValid})", signal, start, value, start.IsValid, value.IsValid);
						distance2 = 0f;
					}
					result.NextSignal = new Found<CTCSignal>(signal, value, distance2);
					break;
				}
			}
		}
		foreach (TrackMarker item2 in graph.EnumerateTrackMarkers(start, result.AvailableDistance, sameDirection: false))
		{
			TrackMarker trackMarker = item2;
			switch (trackMarker.type)
			{
			case TrackMarkerType.Flare:
			{
				if (!result.NextFlare.HasValue && FlareManager.TryGetFlarePickable(trackMarker, out var flarePickable))
				{
					result.NextFlare = new Found<string>(flarePickable.FlareId, trackMarker.Location.Value, DistanceToMarker());
				}
				break;
			}
			case TrackMarkerType.Crossing:
				if (!result.NextCrossingDistance.HasValue)
				{
					result.NextCrossingDistance = DistanceToMarker();
				}
				break;
			case TrackMarkerType.PassengerStop:
				if (!result.NextPassengerStop.HasValue)
				{
					result.NextPassengerStop = new Found<PassengerStop>(trackMarker.PassengerStop, trackMarker.Location.Value, DistanceToMarker());
				}
				break;
			default:
				throw new ArgumentOutOfRangeException();
			case TrackMarkerType.Generic:
			case TrackMarkerType.Signal:
				break;
			}
			float DistanceToMarker()
			{
				return graph.GetDistanceBetweenClose(start, trackMarker.Location.Value);
			}
		}
		if (result.AvailableDistance == 0f)
		{
			result.AverageGrade = graph.GradeAtLocation(cursor);
		}
		else
		{
			result.AverageGrade /= result.AvailableDistance / 10f;
		}
		float MaxSpeedForTrackMph(Location location2)
		{
			float num9 = TrainMath.MaximumSpeedMphForCurve(graph.CurvatureAtLocation(location2, Graph.CurveQueryResolution.Segment), equipmentMaximumTrackCurvature);
			num9 = Mathf.Max(5f, RoundDown(Mathf.Max(num9 - 3f, num9 * 0.8f)));
			int num10 = location2.segment.speedLimit;
			if (num10 == 0)
			{
				num10 = 35;
			}
			return Mathf.Min(num9, num10);
		}
		static float RoundDown(float num9, int nearest = 5)
		{
			return Mathf.FloorToInt(num9 / (float)nearest) * nearest;
		}
		bool ShouldConsiderCar(Car other)
		{
			if (coupledCars.Contains(other))
			{
				return false;
			}
			return true;
		}
	}

	private static bool CheckThrowable(TrackNode node, TrainController trainController, ICollection<Car> coupledCars, out StopAnnounce stopAnnounce)
	{
		if (!trainController.CanSetSwitch(node, !node.isThrown, out var foundCar) && !coupledCars.Contains(foundCar))
		{
			stopAnnounce = StopAnnounce.SwitchFouled;
			return false;
		}
		if (node.IsCTCSwitch && !node.IsCTCSwitchUnlocked)
		{
			stopAnnounce = StopAnnounce.CTCSwitchLocked;
			return false;
		}
		stopAnnounce = StopAnnounce.SwitchAgainst;
		return true;
	}

	private static float DistanceToCar(Location location, Car car, Graph graph)
	{
		Vector3 position = graph.GetPosition(location);
		return Mathf.Min(Vector3.Distance(position, graph.GetPosition(car.LocationF)), Vector3.Distance(position, graph.GetPosition(car.LocationR)));
	}
}
