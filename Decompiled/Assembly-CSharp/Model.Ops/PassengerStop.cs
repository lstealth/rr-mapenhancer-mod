using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Analytics;
using Core;
using GalaSoft.MvvmLight.Messaging;
using Game;
using Game.AccessControl;
using Game.Events;
using Game.Reputation;
using Game.State;
using Helpers;
using KeyValue.Runtime;
using Model.Definition.Data;
using Model.Ops.Definition;
using Model.Ops.Timetable;
using Network;
using Serilog;
using Track;
using Track.Search;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;

namespace Model.Ops;

public class PassengerStop : GameBehaviour, IIndustryTrackDisplayable, IProgressionDisablable
{
	public readonly struct WaitingInfo
	{
		public readonly IReadOnlyList<WaitingPassengerGroup> Groups;

		public readonly int Total;

		public WaitingInfo(IReadOnlyList<WaitingPassengerGroup> groups)
		{
			Groups = groups;
			Total = 0;
			foreach (WaitingPassengerGroup group in Groups)
			{
				Total += group.Count;
			}
		}

		public static WaitingInfo FromPropertyValue(Value value)
		{
			IReadOnlyList<Value> arrayValue = value["groups"].ArrayValue;
			List<WaitingPassengerGroup> list = new List<WaitingPassengerGroup>(arrayValue.Count);
			foreach (Value item2 in arrayValue)
			{
				WaitingPassengerGroup item = WaitingPassengerGroup.FromPropertyValue(item2);
				if (item.Count != 0)
				{
					list.Add(item);
				}
			}
			return new WaitingInfo(list);
		}

		public Value PropertyValue()
		{
			List<Value> list = new List<Value>(Groups.Count);
			foreach (WaitingPassengerGroup group in Groups)
			{
				list.Add(group.PropertyValue());
			}
			return Value.Dictionary(new Dictionary<string, Value> { 
			{
				"groups",
				Value.Array(list)
			} });
		}
	}

	private class PendingPayment
	{
		public float LastPaymentTime;

		public int Count;

		public float Amount;
	}

	public struct DistanceInfo
	{
		public float DistanceInMiles;

		public float TraverseTimeSeconds;

		public readonly bool Success;

		public DistanceInfo(float distanceInMiles, float traverseTimeSeconds, bool success)
		{
			DistanceInMiles = distanceInMiles;
			TraverseTimeSeconds = traverseTimeSeconds;
			Success = success;
		}
	}

	public string identifier;

	public string timetableCode;

	[Range(0f, 100f)]
	[FormerlySerializedAs("waitingCapacity")]
	public int basePopulation = 50;

	public bool flagStop;

	public PassengerStop[] neighbors;

	public Load passengerLoad;

	private TrackSpan[] _spans;

	private KeyValueObject _keyValueObject;

	private IDisposable _observer;

	private static readonly string[] PassengerStopSuffixes = new string[2] { "Depot", "Station" };

	private string _timetableName;

	private static PassengerStop[] _allPassengerStops;

	private readonly Dictionary<string, WaitingInfo> _waiting = new Dictionary<string, WaitingInfo>();

	private GameDateTime _lastGrow;

	private Coroutine _loop;

	private readonly HashSet<string> _workingCarIds = new HashSet<string>();

	private HashSet<PassengerStop> _availableDestinations;

	private const int GrowInterval = 300;

	private const string StateKey = "state";

	internal const double GroupWindowSeconds = 600.0;

	private HashSet<TrackMarker> _markers;

	private readonly PendingPayment _pendingPayment = new PendingPayment();

	private static readonly Dictionary<string, DistanceInfo> MilesBetweenPassengerStops = new Dictionary<string, DistanceInfo>();

	private readonly float[] _levelsByHour = new float[24]
	{
		0.2f, 0.1f, 0.1f, 0.3f, 0.4f, 0.7f, 1f, 1f, 1f, 1f,
		0.8f, 0.6f, 0.5f, 0.6f, 0.5f, 0.6f, 0.8f, 1f, 1f, 0.8f,
		0.5f, 0.4f, 0.3f, 0.2f
	};

	public IReadOnlyDictionary<string, WaitingInfo> Waiting => _waiting;

	public bool ProgressionDisabled { get; set; }

	public int AdditionalPopulation { get; set; }

	private int MaxWaiting => basePopulation + AdditionalPopulation;

	public string TimetableName
	{
		get
		{
			if (_timetableName == null)
			{
				_timetableName = RemoveSuffixes(DisplayName);
			}
			return _timetableName;
			static string RemoveSuffixes(string input)
			{
				if (string.IsNullOrWhiteSpace(input))
				{
					return input;
				}
				string[] passengerStopSuffixes = PassengerStopSuffixes;
				foreach (string text in passengerStopSuffixes)
				{
					if (input.EndsWith(text))
					{
						input = input.Substring(0, input.Length - text.Length);
					}
				}
				return input.Trim();
			}
		}
	}

	private static GameDateTime Now => TimeWeather.Now;

	private string KeyValueIdentifier => "pass." + identifier;

	private IEnumerable<PassengerStop> ActiveAvailableDestinations => _availableDestinations.Where((PassengerStop ps) => !ps.ProgressionDisabled);

	private static float PaymentTime => Time.unscaledTime;

	public string DisplayName => base.name;

	public bool IsVisible => true;

	public IEnumerable<TrackSpan> TrackSpans => _spans;

	public Vector3 CenterPoint
	{
		get
		{
			if (_spans.Length != 0)
			{
				return _spans[0].GetCenterPoint();
			}
			return WorldTransformer.WorldToGame(base.transform.position);
		}
	}

	public GameDateTime? LastServed => ReputationTracker.Shared.LastServed(identifier);

	public static IEnumerable<PassengerStop> FindAll()
	{
		if (_allPassengerStops == null || _allPassengerStops.FirstOrDefault() == null)
		{
			_allPassengerStops = UnityEngine.Object.FindObjectsOfType<PassengerStop>();
		}
		return _allPassengerStops;
	}

	private void Awake()
	{
		_keyValueObject = base.gameObject.AddComponent<KeyValueObject>();
		StateManager.Shared.RegisterPropertyObject(KeyValueIdentifier, _keyValueObject, AuthorizationRequirement.HostOnly);
	}

	private void OnDestroy()
	{
		if (StateManager.Shared != null)
		{
			StateManager.Shared.UnregisterPropertyObject(KeyValueIdentifier);
		}
	}

	protected override void OnEnable()
	{
		_availableDestinations = (from ps in UnityEngine.Object.FindObjectsOfType<PassengerStop>()
			where ps != this
			select ps).ToHashSet();
		_spans = GetComponentsInChildren<TrackSpan>();
		_markers = (from marker in _spans.Select(delegate(TrackSpan span)
			{
				if (span.lower.HasValue && span.upper.HasValue)
				{
					GameObject gameObject = new GameObject("PassengerStopMarker");
					gameObject.SetActive(value: false);
					gameObject.transform.parent = base.transform;
					TrackMarker trackMarker = gameObject.AddComponent<TrackMarker>();
					trackMarker.type = TrackMarkerType.PassengerStop;
					trackMarker.Location = Graph.Shared.Lerp(span.lower.Value, span.upper.Value, 0.5f);
					gameObject.SetActive(value: true);
					return trackMarker;
				}
				return (TrackMarker)null;
			})
			where marker != null
			select marker).ToHashSet();
		base.OnEnable();
	}

	protected override void OnEnableWithProperties()
	{
		if (StateManager.IsHost)
		{
			LoadState();
		}
		else
		{
			_observer = _keyValueObject.Observe("state", delegate
			{
				LoadState();
			});
		}
		if (StateManager.IsHost)
		{
			_loop = StartCoroutine(Loop());
		}
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		if (_markers != null)
		{
			foreach (TrackMarker marker in _markers)
			{
				UnityEngine.Object.Destroy(marker.gameObject);
			}
			_markers = null;
		}
		_observer?.Dispose();
		_observer = null;
		if (_loop != null)
		{
			StopCoroutine(_loop);
			_loop = null;
		}
	}

	private IEnumerator Loop()
	{
		TrainController trainController = TrainController.Shared;
		while (true)
		{
			yield return new WaitForSeconds(3f);
			if (ProgressionDisabled)
			{
				continue;
			}
			foreach (Car item in FindCars(trainController))
			{
				if (!_workingCarIds.Contains(item.id) && ShouldWorkCar(item))
				{
					StartCoroutine(WorkCar(item));
				}
			}
			GrowWaiting();
			PayPending();
		}
	}

	private bool ShouldWorkCar(Car car)
	{
		return IsStopped(car);
	}

	private static PassengerMarker MarkerForCar(Car car)
	{
		return car.GetPassengerMarker() ?? PassengerMarker.Empty();
	}

	private int PassengerCapacity(Car car)
	{
		return (int)car.Definition.LoadSlots.First((LoadSlot slot) => slot.LoadRequirementsMatch(passengerLoad)).MaximumCapacity;
	}

	private PassengerStop RandomDestinationWeighted(List<PassengerStop> currentDestinations)
	{
		if (currentDestinations.Count == 0)
		{
			return null;
		}
		int maxExclusive = currentDestinations.Sum((PassengerStop ps) => ps.MaxWaiting);
		int num = UnityEngine.Random.Range(0, maxExclusive);
		foreach (PassengerStop currentDestination in currentDestinations)
		{
			int maxWaiting = currentDestination.MaxWaiting;
			if (maxWaiting > num)
			{
				return currentDestination;
			}
			num -= maxWaiting;
		}
		return currentDestinations.Last();
	}

	private int CalculateMaxWaiting()
	{
		float num = (float)basePopulation / (float)ActiveAvailableDestinations.Sum((PassengerStop d) => d.basePopulation);
		int num2 = Mathf.RoundToInt((float)ActiveAvailableDestinations.Sum((PassengerStop d) => d.AdditionalPopulation) * num);
		return MaxWaiting + num2;
	}

	private void GrowWaiting()
	{
		GameDateTime now = Now;
		int num = (int)((now - _lastGrow) / 300.0);
		if (num >= 0)
		{
			if (num == 0)
			{
				return;
			}
		}
		else
		{
			Log.Warning("Negative cycles: {now} - {lastGrow} = {delta}", now, _lastGrow, now - _lastGrow);
			_lastGrow = now.AddingSeconds(-300f);
			num = 1;
		}
		float num2 = _levelsByHour[(int)now.Hours];
		int num3 = CalculateMaxWaiting();
		float maxWaitingCoefficient;
		float growthChance;
		List<PassengerStop> list = CalculateWeightedAvailableDestinations(now, out maxWaitingCoefficient, out growthChance);
		int num4 = Mathf.RoundToInt((float)num3 * maxWaitingCoefficient * num2);
		HashSet<string> hashSet = CollectionPool<HashSet<string>, string>.Get();
		foreach (PassengerStop item in list)
		{
			hashSet.Add(item.identifier);
		}
		for (int i = 0; i < num; i++)
		{
			ThinWaitingNotFoundInWeighted(hashSet);
		}
		int num5 = Mathf.CeilToInt((float)num4 * 0.5f);
		for (int j = 0; j < num; j++)
		{
			int num6 = TotalWaitingFromHere();
			float value = UnityEngine.Random.value;
			int num7 = ((num4 != num6) ? ((num4 <= num6) ? (-Mathf.CeilToInt(value * (float)num5)) : Mathf.Min(num4 - num6, Mathf.RoundToInt(value * growthChance * (float)num5))) : 0);
			if (num7 > 0)
			{
				if (list.Count == 0)
				{
					break;
				}
				while (num7 > 0)
				{
					PassengerStop passengerStop = RandomDestinationWeighted(list);
					int num8 = Mathf.Min(num7, 3);
					OffsetWaiting(passengerStop.identifier, identifier, now, num8);
					num7 -= num8;
				}
				continue;
			}
			if (num6 <= 0)
			{
				break;
			}
			List<string> list2 = (from pair in _waiting
				where pair.Value.Total > 0
				select pair.Key).Distinct().ToList();
			string destination = list2[UnityEngine.Random.Range(0, list2.Count)];
			OffsetWaiting(destination, identifier, now, num7);
		}
		CollectionPool<HashSet<string>, string>.Release(hashSet);
		_lastGrow = _lastGrow.AddingSeconds(300 * num);
		SaveState();
	}

	private int TotalWaitingFromHere()
	{
		int num = 0;
		foreach (WaitingInfo value in _waiting.Values)
		{
			foreach (WaitingPassengerGroup group in value.Groups)
			{
				if (!(group.Origin != identifier))
				{
					num += group.Count;
				}
			}
		}
		return num;
	}

	public int GetTotalWaitingForDestination(string destination)
	{
		if (!_waiting.TryGetValue(destination, out var value))
		{
			return 0;
		}
		return value.Total;
	}

	private void ThinWaitingNotFoundInWeighted(HashSet<string> uniqueDestinationIds)
	{
		List<string> list = CollectionPool<List<string>, string>.Get();
		List<(string, WaitingPassengerGroup)> list2 = CollectionPool<List<(string, WaitingPassengerGroup)>, (string, WaitingPassengerGroup)>.Get();
		try
		{
			int num = 0;
			string key;
			WaitingInfo value;
			foreach (KeyValuePair<string, WaitingInfo> item2 in _waiting)
			{
				item2.Deconstruct(out key, out value);
				string item = key;
				WaitingInfo waitingInfo = value;
				if (!uniqueDestinationIds.Contains(item))
				{
					list.Add(item);
					num += waitingInfo.Total;
				}
			}
			num = Mathf.CeilToInt((float)num * 0.1f);
			foreach (KeyValuePair<string, WaitingInfo> item3 in _waiting)
			{
				item3.Deconstruct(out key, out value);
				string text = key;
				WaitingInfo waitingInfo2 = value;
				if (waitingInfo2.Total <= 0 || !list.Contains(text))
				{
					continue;
				}
				foreach (WaitingPassengerGroup group in waitingInfo2.Groups)
				{
					if (!(group.Origin != identifier))
					{
						list2.Add((text, group));
					}
				}
			}
			int num2 = num * 2;
			while (num > 0 && num2 > 0 && list2.Count > 0)
			{
				int index = UnityEngine.Random.Range(0, list2.Count);
				var (text2, waitingPassengerGroup) = list2[index];
				if (OffsetWaiting(text2, waitingPassengerGroup.Origin, waitingPassengerGroup.Boarded, -1))
				{
					num--;
				}
				if (!_waiting.TryGetValue(text2, out var value2) || value2.Total <= 0)
				{
					list2.RemoveAt(index);
				}
				num2--;
			}
		}
		finally
		{
			CollectionPool<List<string>, string>.Release(list);
			CollectionPool<List<(string, WaitingPassengerGroup)>, (string, WaitingPassengerGroup)>.Release(list2);
		}
	}

	private List<PassengerStop> CalculateWeightedAvailableDestinations(GameDateTime now, out float maxWaitingCoefficient, out float growthChance)
	{
		TimetableController shared = TimetableController.Shared;
		Model.Ops.Timetable.Timetable current = shared.Current;
		maxWaitingCoefficient = 1f;
		growthChance = 1f;
		if (current == null || !shared.HasPassengerTrains)
		{
			return GetDefault();
		}
		if (string.IsNullOrEmpty(timetableCode))
		{
			return GetDefault();
		}
		return GetTimetableDestinations(shared, current, now, timetableCode, out maxWaitingCoefficient, out growthChance);
		List<PassengerStop> GetDefault()
		{
			return ActiveAvailableDestinations.ToList();
		}
	}

	private static List<PassengerStop> GetTimetableDestinations(TimetableController timetableController, Model.Ops.Timetable.Timetable timetable, GameDateTime now, string timetableCode, out float maxWaitingCoefficient, out float growthChance)
	{
		Config shared = Config.Shared;
		PassengerStopTimetableLogic.GetTimetableDestinationsConfig config = new PassengerStopTimetableLogic.GetTimetableDestinationsConfig
		{
			MinimumStopDuration = StateManager.Shared.Storage.AIPassengerStopMinimumStopDuration,
			DepartureImmediacyToCoefficient = shared.passengerDepartureImmediacyToCoefficient,
			DepartureImmediacyGrowthChance = shared.passengerDepartureImmediacyGrowthChance,
			DepartureImmediacyToMultiplier = shared.passengerDepartureImmediacyToMultiplier,
			DeparturePastToCoefficient = shared.passengerDeparturePastToCoefficient
		};
		List<string> timetableDestinations = PassengerStopTimetableLogic.GetTimetableDestinations(timetable, now, timetableCode, config, out maxWaitingCoefficient, out growthChance);
		List<PassengerStop> list = new List<PassengerStop>();
		foreach (string item in timetableDestinations)
		{
			if (timetableController.TryGetPassengerStop(item, out var passengerStop))
			{
				list.Add(passengerStop);
			}
		}
		return list;
	}

	private void SaveState()
	{
		Dictionary<string, Value> dictionary = new Dictionary<string, Value>();
		Dictionary<string, Value> dictionary2 = new Dictionary<string, Value>();
		foreach (var (key, waitingInfo2) in _waiting)
		{
			if (waitingInfo2.Total > 0)
			{
				dictionary2[key] = waitingInfo2.PropertyValue();
			}
		}
		dictionary["waiting"] = Value.Dictionary(dictionary2);
		dictionary["lastGrow"] = Value.Int((int)_lastGrow.TotalSeconds);
		_keyValueObject["state"] = Value.Dictionary(dictionary);
	}

	private void LoadState()
	{
		_lastGrow = Now.AddingSeconds(-300f);
		IReadOnlyDictionary<string, Value> dictionaryValue = _keyValueObject["state"].DictionaryValue;
		if (!dictionaryValue.Any())
		{
			return;
		}
		try
		{
			_waiting.Clear();
			Value value = dictionaryValue["waiting"];
			foreach (KeyValuePair<string, Value> item in value.DictionaryValue)
			{
				item.Deconstruct(out var key, out value);
				string key2 = key;
				Value value2 = value;
				_waiting[key2] = WaitingInfo.FromPropertyValue(value2);
			}
			Value value3 = dictionaryValue["lastGrow"];
			_lastGrow = (value3.IsNull ? Now : new GameDateTime(value3.IntValue));
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Exception in LoadState {identifier}", identifier);
		}
	}

	private IEnumerator WorkCar(Car car)
	{
		string carId = car.id;
		_workingCarIds.Add(carId);
		car.CopyStopsFromTimetable(onlyIfAuto: true);
		while (car != null && ShouldWorkCar(car))
		{
			if (UnloadCar(car))
			{
				yield return new WaitForSeconds(UnityEngine.Random.Range(1f, 2f));
				continue;
			}
			RemoveDestinationFromMarker(car);
			if (!LoadCar(car))
			{
				break;
			}
			yield return new WaitForSeconds(UnityEngine.Random.Range(1f, 2f));
		}
		_workingCarIds.Remove(carId);
	}

	private static float CalculateBonusMultiplier(Car car)
	{
		List<Car> source = car.EnumerateCoupled().ToList();
		Car target = source.First();
		Car target2 = source.Last();
		if (CheckObservationTrailing(target, Car.LogicalEnd.A) || CheckObservationTrailing(target2, Car.LogicalEnd.B))
		{
			return 1.2f;
		}
		return 1f;
		static bool CheckObservationTrailing(Car car2, Car.LogicalEnd logicalEnd)
		{
			if (car2.CarType != "PBO")
			{
				return false;
			}
			if (car2.LogicalToEnd(logicalEnd) == Car.End.F)
			{
				return false;
			}
			return car2.CoupledTo(logicalEnd) == null;
		}
	}

	private void RemoveDestinationFromMarker(Car car)
	{
		PassengerMarker value = MarkerForCar(car);
		if (value.Destinations.Contains(identifier))
		{
			value.Destinations.Remove(identifier);
			car.SetPassengerMarker(value);
		}
	}

	private bool UnloadCar(Car car)
	{
		float bonusMultiplier = CalculateBonusMultiplier(car);
		PassengerMarker value = MarkerForCar(car);
		bool flag = string.IsNullOrEmpty(value.LastStopIdentifier);
		if (flag || value.LastStopIdentifier != identifier)
		{
			if (!flag)
			{
				FirePassengerStopEdgeMoved(value.LastStopIdentifier);
			}
			value.LastStopIdentifier = identifier;
			car.SetPassengerMarker(value);
		}
		if (value.TryRemovePassenger(identifier, out var removedDestination, out var removedOrigin, out var removedBoarded))
		{
			car.SetPassengerMarker(value);
			if (removedDestination == identifier)
			{
				QueuePayment(1, removedOrigin, identifier, bonusMultiplier);
				FirePassengerStopServed(1, car.Condition);
			}
			else
			{
				OffsetWaiting(removedDestination, removedOrigin, removedBoarded, 1);
				SaveState();
				FirePassengerStopServed(-1, car.Condition);
			}
			return true;
		}
		return false;
	}

	private bool OffsetWaiting(string destination, string origin, GameDateTime sourceGroupBoarded, int delta)
	{
		if (delta == 0)
		{
			return false;
		}
		WaitingInfo value = default(WaitingInfo);
		if (_waiting.TryGetValue(destination, out var value2))
		{
			bool flag = false;
			for (int i = 0; i < value2.Groups.Count; i++)
			{
				WaitingPassengerGroup waitingPassengerGroup = value2.Groups[i];
				if (Matches(waitingPassengerGroup, origin, sourceGroupBoarded, identifier))
				{
					waitingPassengerGroup.Count += delta;
					List<WaitingPassengerGroup> list = new List<WaitingPassengerGroup>(value2.Groups);
					if (waitingPassengerGroup.Count > 0)
					{
						list[i] = waitingPassengerGroup;
					}
					else
					{
						list.RemoveAt(i);
					}
					value = new WaitingInfo(list);
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				if (delta < 0)
				{
					return false;
				}
				List<WaitingPassengerGroup> groups = new List<WaitingPassengerGroup>(value2.Groups)
				{
					new WaitingPassengerGroup(origin, delta, sourceGroupBoarded)
				};
				value = new WaitingInfo(groups);
			}
		}
		else
		{
			if (delta < 0)
			{
				return false;
			}
			value = new WaitingInfo(new List<WaitingPassengerGroup>
			{
				new WaitingPassengerGroup(origin, delta, sourceGroupBoarded)
			});
		}
		if (value.Total > 0)
		{
			_waiting[destination] = value;
		}
		else
		{
			_waiting.Remove(destination);
		}
		return true;
		static bool Matches(WaitingPassengerGroup group, string checkOrigin, GameDateTime boarded, string thisIdentifier)
		{
			if (group.Origin != checkOrigin)
			{
				return false;
			}
			if (thisIdentifier == checkOrigin)
			{
				return true;
			}
			return (double)Mathf.Abs((float)(group.Boarded - boarded)) < 600.0;
		}
	}

	public int ExpirePassengers(GameDateTime expiration)
	{
		int num = 0;
		Dictionary<string, WaitingInfo> dictionary = new Dictionary<string, WaitingInfo>();
		List<WaitingPassengerGroup> list = new List<WaitingPassengerGroup>();
		string key;
		WaitingInfo value;
		foreach (KeyValuePair<string, WaitingInfo> item in _waiting)
		{
			item.Deconstruct(out key, out value);
			string key2 = key;
			WaitingInfo waitingInfo = value;
			list.Clear();
			list.AddRange(waitingInfo.Groups);
			for (int num2 = waitingInfo.Groups.Count - 1; num2 >= 0; num2--)
			{
				WaitingPassengerGroup waitingPassengerGroup = waitingInfo.Groups[num2];
				if (!(waitingPassengerGroup.Origin == identifier) && !(waitingPassengerGroup.Boarded > expiration))
				{
					list.RemoveAt(num2);
					num += waitingPassengerGroup.Count;
				}
			}
			if (list.Count != waitingInfo.Groups.Count)
			{
				dictionary[key2] = new WaitingInfo(list.ToList());
			}
		}
		if (dictionary.Count <= 0)
		{
			return 0;
		}
		foreach (KeyValuePair<string, WaitingInfo> item2 in dictionary)
		{
			item2.Deconstruct(out key, out value);
			string key3 = key;
			WaitingInfo value2 = value;
			if (value2.Total == 0)
			{
				_waiting.Remove(key3);
			}
			else
			{
				_waiting[key3] = value2;
			}
		}
		SaveState();
		return num;
	}

	private bool LoadCar(Car car)
	{
		PassengerMarker value = MarkerForCar(car);
		int num = PassengerCapacity(car) - value.TotalPassengers;
		if (num <= 0)
		{
			return false;
		}
		string destinationOut;
		string originOut;
		GameDateTime boardedOut;
		int num2 = AllocateWaitingPassengersForDestinations(num, value.Destinations, out destinationOut, out originOut, out boardedOut);
		if (num2 <= 0)
		{
			return false;
		}
		float condition = car.Condition;
		value.AddPassengers(originOut, destinationOut, num2, boardedOut);
		car.SetPassengerMarker(value);
		SaveState();
		FirePassengerStopServed(num2, condition);
		return true;
	}

	private int AllocateWaitingPassengersForDestinations(int maximum, HashSet<string> destinations, out string destinationOut, out string originOut, out GameDateTime boardedOut)
	{
		destinationOut = null;
		originOut = null;
		boardedOut = Now;
		try
		{
			WaitingPassengerGroup? waitingPassengerGroup = null;
			string text = null;
			foreach (var (text3, waitingInfo2) in _waiting)
			{
				if (!destinations.Contains(text3))
				{
					continue;
				}
				foreach (WaitingPassengerGroup group in waitingInfo2.Groups)
				{
					GameDateTime gameDateTime = group.Boarded;
					if (group.Origin == identifier)
					{
						gameDateTime = Now;
					}
					if (!waitingPassengerGroup.HasValue || gameDateTime < waitingPassengerGroup.Value.Boarded)
					{
						waitingPassengerGroup = group;
						text = text3;
						boardedOut = gameDateTime;
					}
				}
			}
			if (waitingPassengerGroup.HasValue)
			{
				WaitingPassengerGroup valueOrDefault = waitingPassengerGroup.GetValueOrDefault();
				destinationOut = text;
				originOut = valueOrDefault.Origin;
				int num = Mathf.Min(Mathf.Min(maximum, destinationOut.Length), UnityEngine.Random.Range(1, 3));
				if (num <= 0)
				{
					return 0;
				}
				OffsetWaiting(destinationOut, valueOrDefault.Origin, valueOrDefault.Boarded, -num);
				return num;
			}
			return 0;
		}
		catch
		{
			destinationOut = null;
			return 0;
		}
	}

	private void FirePassengerStopServed(int positiveOrNegative, float carCondition)
	{
		Messenger.Default.Send(new PassengerStopServed(identifier, positiveOrNegative, carCondition));
	}

	private void FirePassengerStopEdgeMoved(string originIdentifier)
	{
		if (neighbors.Any((PassengerStop n) => n.identifier == originIdentifier))
		{
			Messenger.Default.Send(new PassengerStopEdgeMoved(originIdentifier, identifier));
			return;
		}
		List<string> list = FindPath(this, originIdentifier);
		if (list == null || list.Last() != originIdentifier)
		{
			Log.Error("Path from {a} to {b} not found - PassengerStopEdgeMoved will not be fired", identifier, originIdentifier);
			return;
		}
		for (int num = 0; num < list.Count - 1; num++)
		{
			string text = list[num];
			string to = list[num + 1];
			Messenger.Default.Send(new PassengerStopEdgeMoved(text, to));
		}
	}

	[ContextMenu("Test: Alarka")]
	private void TestFindAdjacent()
	{
		List<string> values = FindPath(this, "alarka");
		Debug.Log(string.Join(", ", values));
	}

	private IEnumerable<(string, string)> FindAdjacentPassengerStops(string originIdentifier)
	{
		HashSet<(string, string)> output = new HashSet<(string, string)>();
		HashSet<string> visited = new HashSet<string> { identifier };
		PassengerStop[] array = neighbors;
		for (int i = 0; i < array.Length && !Search(array[i], identifier); i++)
		{
		}
		return output;
		bool Search(PassengerStop ps, string from)
		{
			visited.Add(ps.identifier);
			if (ps.identifier == originIdentifier)
			{
				output.Add((ps.identifier, from));
				return true;
			}
			PassengerStop[] array2 = ps.neighbors;
			foreach (PassengerStop passengerStop in array2)
			{
				if (!visited.Contains(passengerStop.identifier) && Search(passengerStop, ps.identifier))
				{
					output.Add((ps.identifier, from));
					return true;
				}
			}
			return false;
		}
	}

	private static List<string> FindPath(PassengerStop start, string end)
	{
		List<string> path = new List<string>(16);
		return _FindPath(start, end, path);
	}

	private static List<string> _FindPath(PassengerStop start, string end, List<string> path)
	{
		path.Add(start.identifier);
		if (start.identifier == end)
		{
			return path;
		}
		List<string> list = null;
		PassengerStop[] array = start.neighbors;
		foreach (PassengerStop passengerStop in array)
		{
			if (!passengerStop.ProgressionDisabled && !path.Contains(passengerStop.identifier))
			{
				List<string> path2 = new List<string>(path);
				List<string> list2 = _FindPath(passengerStop, end, path2);
				if (list2 != null && (list == null || list.Count > list2.Count))
				{
					list = list2;
				}
			}
		}
		return list;
	}

	private void QueuePayment(int count, string originId, string destinationId, float bonusMultiplier)
	{
		if (TryCalculateMilesBetweenPassengerStops(originId, destinationId, out var distanceInfo))
		{
			float distanceInMiles = distanceInfo.DistanceInMiles;
			int num = Mathf.RoundToInt(Mathf.Lerp(1f, 8f, Mathf.InverseLerp(2f, 50f, distanceInMiles)));
			float num2 = (float)num * bonusMultiplier;
			Log.Debug("Fare: {a} to {b}: {miles} {maximum} {fare}", originId, destinationId, distanceInMiles, num, num2);
			_pendingPayment.Count += count;
			_pendingPayment.Amount += num2;
			_pendingPayment.LastPaymentTime = PaymentTime;
		}
	}

	private void PayPending()
	{
		float num = PaymentTime - 5f;
		if (!(_pendingPayment.LastPaymentTime > num) && _pendingPayment.Count != 0)
		{
			PayPassengerFare(_pendingPayment.Count, Mathf.CeilToInt(_pendingPayment.Amount));
			_pendingPayment.Count = 0;
			_pendingPayment.Amount = 0f;
		}
	}

	private void PayPassengerFare(int numberOfPassengers, int amount)
	{
		if (amount != 0)
		{
			StateManager.Shared.ApplyToBalance(amount, Ledger.Category.Passenger, new EntityReference(EntityType.PassengerStop, identifier), null, numberOfPassengers, quiet: true);
			Hyperlink hyperlink = Hyperlink.To(this);
			string text = string.Format("{0}: Received passenger ticket fare: {1:C0} for {2}", hyperlink, amount, numberOfPassengers.Pluralize("fare"));
			Log.Debug(text);
			Multiplayer.Broadcast(text);
			global::Analytics.Analytics.Post("PassengerPayment", new Dictionary<string, object>
			{
				{ "id", identifier },
				{ "amount", amount },
				{ "numberOfPassengers", numberOfPassengers }
			});
		}
	}

	public static bool TryCalculateMilesBetweenPassengerStops(string idA, string idB, out DistanceInfo distanceInfo)
	{
		distanceInfo = default(DistanceInfo);
		if (idA == idB)
		{
			return false;
		}
		if (string.Compare(idA, idB, StringComparison.Ordinal) > 0)
		{
			string text = idB;
			string text2 = idA;
			idA = text;
			idB = text2;
		}
		string key = idA + "--" + idB;
		if (MilesBetweenPassengerStops.TryGetValue(key, out distanceInfo))
		{
			return distanceInfo.Success;
		}
		PassengerStop passengerStopForId = GetPassengerStopForId(idA);
		PassengerStop passengerStopForId2 = GetPassengerStopForId(idB);
		if (passengerStopForId == null || passengerStopForId2 == null)
		{
			return false;
		}
		Location? lower = passengerStopForId.TrackSpans.First().lower;
		Location? lower2 = passengerStopForId2.TrackSpans.First().lower;
		if (!lower.HasValue || !lower2.HasValue)
		{
			Log.Error("Couldn't find location for {a} or {b}", idA, idB);
			return false;
		}
		try
		{
			float totalDistance;
			float traverseTimeSeconds;
			bool flag = TrainController.Shared.graph.TryFindDistance(lower.Value, lower2.Value, out totalDistance, out traverseTimeSeconds);
			distanceInfo = new DistanceInfo(totalDistance * 0.0006213712f, traverseTimeSeconds, flag);
			MilesBetweenPassengerStops[key] = distanceInfo;
			return flag;
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Exception while finding distance between {a} and {b}", idA, idB);
			return false;
		}
	}

	private static PassengerStop GetPassengerStopForId(string id)
	{
		foreach (PassengerStop item in FindAll())
		{
			if (item.identifier == id)
			{
				return item;
			}
		}
		return null;
	}

	private HashSet<Car> FindCars(TrainController trainController)
	{
		HashSet<Car> hashSet = _spans.SelectMany(trainController.CarsOnSpan).Where(IsStopped).Where(IsPassengerCar)
			.ToHashSet();
		HashSet<Car> hashSet2 = new HashSet<Car>();
		foreach (Car item in hashSet)
		{
			hashSet2.Add(item);
			foreach (Car item2 in item.EnumerateCoupled())
			{
				if (IsPassengerCar(item2))
				{
					hashSet2.Add(item2);
				}
			}
			foreach (Car item3 in item.EnumerateCoupled(Car.LogicalEnd.B))
			{
				if (IsPassengerCar(item3))
				{
					hashSet2.Add(item3);
				}
			}
		}
		return hashSet2;
	}

	public bool CarIsAtPassengerStop(Car car, TrainController trainController)
	{
		return FindCars(trainController).Contains(car);
	}

	private static bool IsStopped(Car car)
	{
		return car.IsStopped(10f);
	}

	private bool IsPassengerCar(Car car)
	{
		return car.IsPassengerCar();
	}

	public static string NameForIdentifier(string identifier)
	{
		PassengerStop passengerStopForId = GetPassengerStopForId(identifier);
		if (passengerStopForId == null)
		{
			return identifier;
		}
		return passengerStopForId.DisplayName;
	}

	public static string ShortNameForIdentifier(string identifier)
	{
		string text = NameForIdentifier(identifier);
		if (text.EndsWith(" Depot"))
		{
			return text.Replace(" Depot", "");
		}
		if (text.EndsWith(" Station"))
		{
			return text.Replace(" Station", "");
		}
		return text;
	}

	internal bool OffsetWaitingOpsCommand(string destination, string origin, GameDateTime sourceGroupBoarded, int delta)
	{
		bool num = OffsetWaiting(destination, origin, sourceGroupBoarded, delta);
		if (num)
		{
			SaveState();
		}
		return num;
	}

	public void ClearAllWaiting()
	{
		_waiting.Clear();
		SaveState();
	}
}
