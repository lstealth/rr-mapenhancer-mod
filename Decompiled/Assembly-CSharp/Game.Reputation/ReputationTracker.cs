using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Analytics;
using GalaSoft.MvvmLight.Messaging;
using Game.AccessControl;
using Game.Events;
using Game.State;
using KeyValue.Runtime;
using Model.Ops;
using Serilog;
using UI.Console;
using UnityEngine;

namespace Game.Reputation;

public class ReputationTracker : GameBehaviour
{
	private KeyValueObject _keyValueObject;

	private Coroutine _tickCoroutine;

	private const string ObjectId = "_reputation";

	private static ReputationTracker _instance;

	private const string LastDayKey = "lastDay";

	private const string ReputationKey = "total";

	private const string ReputationReportKey = "report";

	private const string KeyDerailmentHours = "derailments";

	private static GameDateTime Now => TimeWeather.Now;

	public static ReputationTracker Shared
	{
		get
		{
			if (_instance == null)
			{
				_instance = UnityEngine.Object.FindObjectOfType<ReputationTracker>();
			}
			return _instance;
		}
	}

	private int PassengerTotal
	{
		get
		{
			return _keyValueObject["pass-total"];
		}
		set
		{
			_keyValueObject["pass-total"] = value;
		}
	}

	private float PassengerCarConditionSum
	{
		get
		{
			return _keyValueObject["pass-cc"];
		}
		set
		{
			_keyValueObject["pass-cc"] = value;
		}
	}

	private int LastUpdatedDay
	{
		get
		{
			return _keyValueObject["lastDay"].IntValue;
		}
		set
		{
			_keyValueObject["lastDay"] = Value.Int(value);
		}
	}

	public float Reputation
	{
		get
		{
			return _keyValueObject["total"].FloatValue;
		}
		private set
		{
			_keyValueObject["total"] = Value.Float(value);
		}
	}

	public ReputationReport Report
	{
		get
		{
			return ReputationReport.FromValue(_keyValueObject["report"]);
		}
		private set
		{
			_keyValueObject["report"] = value.ToValue();
		}
	}

	private void Awake()
	{
		KeyValueObject keyValueObject = base.gameObject.AddComponent<KeyValueObject>();
		StateManager.Shared.RegisterPropertyObject("_reputation", keyValueObject, AuthorizationRequirement.HostOnly);
		_keyValueObject = keyValueObject;
	}

	private void OnDestroy()
	{
		if (StateManager.Shared != null)
		{
			StateManager.Shared.UnregisterPropertyObject("_reputation");
		}
	}

	protected override void OnEnableWithProperties()
	{
		if (StateManager.IsHost)
		{
			Messenger.Default.Register(this, delegate(PassengerStopServed evt)
			{
				PassengerStopServed(evt.Identifier, evt.Offset, evt.CarCondition);
			});
			Messenger.Default.Register(this, delegate(PassengerStopEdgeMoved evt)
			{
				PassengerStopEdgeMoved(evt.From, evt.To);
			});
			Messenger.Default.Register<CarDidDerail>(this, delegate
			{
				CarDidDerail();
			});
			_tickCoroutine = StartCoroutine(TickCoroutine());
		}
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		Messenger.Default.Unregister(this);
		if (_tickCoroutine != null)
		{
			StopCoroutine(_tickCoroutine);
			_tickCoroutine = null;
		}
	}

	private IEnumerator TickCoroutine()
	{
		if (LastUpdatedDay > Now.Day)
		{
			Log.Warning("LastUpdatedDay {lastUpdated} > {nowDay}; resetting.", LastUpdatedDay, Now.Day);
			LastUpdatedDay = Now.Day;
		}
		while (true)
		{
			int lastUpdatedDay = LastUpdatedDay;
			int day = Now.Day;
			if (lastUpdatedDay < day)
			{
				try
				{
					UpdateReputation();
					LastUpdatedDay = day;
				}
				catch (Exception exception)
				{
					Log.Error(exception, "Error updating reputation");
				}
			}
			yield return TimeWeather.WaitForNextDay();
		}
	}

	private void UpdateReputation()
	{
		Log.Debug("Updating reputation:");
		GameDateTime now = Now.WithHours(0f);
		float num;
		try
		{
			num = CalculatePassengerNetworkScore();
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			num = 0f;
		}
		float num2;
		try
		{
			num2 = CalculatePassengerConditionScore();
		}
		catch (Exception exception2)
		{
			Debug.LogException(exception2);
			num2 = 0f;
		}
		RemoveAllEdgeKeys();
		PassengerTotal = 0;
		PassengerCarConditionSum = 0f;
		float num3;
		try
		{
			num3 = CalculateFreightPerformance(now);
		}
		catch (Exception exception3)
		{
			Debug.LogException(exception3);
			num3 = 0f;
		}
		float num4;
		try
		{
			num4 = CalculateSafetyScore(now);
		}
		catch (Exception exception4)
		{
			Debug.LogException(exception4);
			num4 = 0f;
		}
		ReputationReport reputationReport = default(ReputationReport);
		reputationReport.AddComponent(new ReputationReport.Component(0.3f, "Passenger: Network Service", "TODO", Mathf.Clamp01(num)));
		reputationReport.AddComponent(new ReputationReport.Component(0.1f, "Passenger: Equipment Condition", "TODO", Mathf.Clamp01(num2)));
		reputationReport.AddComponent(new ReputationReport.Component(0.4f, "Freight: Performance", "TODO", Mathf.Clamp01(num3)));
		reputationReport.AddComponent(new ReputationReport.Component(0.3f, "Operations: Safety", "TODO", Mathf.Clamp01(num4)));
		float num5 = reputationReport.CalculateOverallReputation();
		float reputation = Reputation;
		Log.Information("Reputation: {oldReputation} -> {newReputation}, Report: {report}", reputation, num5, reputationReport);
		Reputation = num5;
		Report = reputationReport;
		string text = ReputationString(reputation);
		string text2 = ReputationString(num5);
		string text3 = ((text2 == text) ? ("Reputation maintained at " + text2) : ("Reputation has changed from " + text + " to " + text2));
		UI.Console.Console.shared.AddLine(text3);
		StateManager.Shared.SendFireEvent(default(ReputationUpdated));
		global::Analytics.Analytics.Post("ReputationUpdated", new Dictionary<string, object>
		{
			{ "previous", reputation },
			{ "overall", num5 },
			{ "passenger", num },
			{ "passenger-cond", num2 },
			{ "freight", num3 },
			{ "safety", num4 }
		});
	}

	private HashSet<string> GetSetEdgeKeys()
	{
		return _keyValueObject.Keys.Where((string key) => key.StartsWith("pe-")).ToHashSet();
	}

	private void RemoveAllEdgeKeys()
	{
		foreach (string setEdgeKey in GetSetEdgeKeys())
		{
			_keyValueObject[setEdgeKey] = Value.Null();
		}
	}

	[ContextMenu("Calculate Scores")]
	public void TestCalculateScores()
	{
		float num = CalculatePassengerNetworkScore();
		float num2 = CalculateSafetyScore(TimeWeather.Now);
		Debug.Log($"Passenger score: {num:F3}, safety score: {num2:F3}");
	}

	private float CalculatePassengerNetworkScore()
	{
		HashSet<string> playerVisitedEdges = GetSetEdgeKeys();
		GameDateTime now = Now;
		Dictionary<string, PassengerStop> dictionary = PassengerStop.FindAll().Where(IncludePassengerStop).ToDictionary((PassengerStop ps) => ps.identifier, (PassengerStop ps) => ps);
		Dictionary<string, PassengerReputationCalculator.Stop> passengerStops = dictionary.ToDictionary((KeyValuePair<string, PassengerStop> kv) => kv.Key, (KeyValuePair<string, PassengerStop> kv) => new PassengerReputationCalculator.Stop(kv.Key));
		foreach (PassengerReputationCalculator.Stop value in passengerStops.Values)
		{
			value.Neighbors.AddRange(from n in dictionary[value.Id].neighbors.Where(IncludePassengerStop)
				select passengerStops[n.identifier]);
		}
		return PassengerReputationCalculator.Calculate(passengerStops.Values, playerVisitedEdges);
		bool IncludePassengerStop(PassengerStop ps)
		{
			if (!ps.ProgressionDisabled)
			{
				if (!IncludeInNetwork(ps, now))
				{
					return playerVisitedEdges.Any((string edge) => PassengerStopEdgeKeyContains(edge, ps.identifier));
				}
				return true;
			}
			return false;
		}
	}

	private float CalculatePassengerConditionScore()
	{
		int passengerTotal = PassengerTotal;
		float passengerCarConditionSum = PassengerCarConditionSum;
		if (passengerTotal != 0)
		{
			return passengerCarConditionSum / (float)passengerTotal;
		}
		return 0f;
	}

	private float CalculateSafetyScore(GameDateTime now)
	{
		float totalHours = now.AddingDays(-5f).TotalHours;
		List<int> list = DerailmentHoursToDerailmentsPerDay(GetDerailmentHoursAndTrim(totalHours), 5, totalHours);
		Log.Information("Reputation: Counted derailments per day: {count}", list);
		return CalculateSafetyScoreFromDerailmentsPerDay(list);
	}

	public static float CalculateSafetyScoreFromDerailmentsPerDay(List<int> numberOfDerailmentsByDay)
	{
		return CalculateSafetyScoreFromIndividualDayScores(numberOfDerailmentsByDay.Select((int count) => Mathf.InverseLerp(5f, 0f, count)).ToArray());
	}

	private static float CalculateFreightPerformance(GameDateTime now)
	{
		List<Industry> list = (from ind in OpsController.Shared.Areas.SelectMany((Area a) => a.Industries)
			where ind.IncludeInFreightPerformance(now)
			select ind).ToList();
		if (list.Count == 0)
		{
			Log.Debug("Freight Performance: 0 (no industries)");
			return 1f;
		}
		Dictionary<Industry, float> dictionary = list.Where((Industry ind) => ind.PerformanceHistory.Count > 0).ToDictionary((Industry ind) => ind, PerformanceForIndustry);
		float num = ((dictionary.Count == 0) ? 1f : dictionary.Values.Average());
		Log.Debug("Freight Performance: {avg}, values: {values}", num, dictionary);
		return num;
		static float PerformanceForIndustry(Industry industry)
		{
			return industry.PerformanceHistory.OrderByDescending((KeyValuePair<int, float> kv) => kv.Key).First().Value;
		}
	}

	private void PassengerStopServed(string stopIdentifier, int offset, float carCondition)
	{
		int num = Mathf.Abs(offset);
		PassengerTotal += num;
		PassengerCarConditionSum += (float)num * carCondition;
		if (offset > 0)
		{
			GameDateTime now = Now;
			string key = StopHistoryKey(stopIdentifier);
			List<int> list = _keyValueObject[key].ArrayValue.Select((Value v) => v.IntValue).ToList();
			int num2 = list.LastOrDefault();
			int num3 = Mathf.FloorToInt(now.TotalHours);
			if (num3 - num2 > 4)
			{
				list.Add(num3);
				_keyValueObject[key] = Value.Array((IReadOnlyCollection<Value>)(object)list.Select(Value.Int).ToArray());
				Log.Debug("PassengerStopServed: {stopIdentifier}, {history}", stopIdentifier, list);
			}
			_keyValueObject[LastServedKey(stopIdentifier)] = (int)Now.RoundingMinutes(1).TotalSeconds;
		}
	}

	public GameDateTime? LastServed(string stopIdentifier)
	{
		return _keyValueObject[LastServedKey(stopIdentifier)].Optional?.GameDateTime(GameDateTime.Zero);
	}

	public bool IncludeInNetwork(PassengerStop passengerStop, GameDateTime now)
	{
		GameDateTime? gameDateTime = LastServed(passengerStop.identifier);
		if (!gameDateTime.HasValue)
		{
			return false;
		}
		GameDateTime gameDateTime2 = now.AddingDays(-10f);
		return gameDateTime.Value > gameDateTime2;
	}

	private static string StopHistoryKey(string stopIdentifier)
	{
		return "sh-" + stopIdentifier;
	}

	private static string LastServedKey(string stopIdentifier)
	{
		return "ls-" + stopIdentifier;
	}

	private void PassengerStopEdgeMoved(string fromId, string toId)
	{
		string key = KeyForPassengerStopEdge(fromId, toId);
		_keyValueObject[key] = Value.Bool(value: true);
	}

	public static string KeyForPassengerStopEdge(string fromId, string toId)
	{
		if (string.Compare(fromId, toId, StringComparison.Ordinal) < 0)
		{
			string text = toId;
			string text2 = fromId;
			fromId = text;
			toId = text2;
		}
		return "pe--" + fromId + "--" + toId;
	}

	private static bool PassengerStopEdgeKeyContains(string edgeKey, string id)
	{
		if (!edgeKey.Contains(id))
		{
			return false;
		}
		string[] array = edgeKey.Split("--");
		if (array.Length != 3)
		{
			return false;
		}
		if (!(array[1] == id))
		{
			return array[2] == id;
		}
		return true;
	}

	private void CarDidDerail()
	{
		List<Value> list = _keyValueObject["derailments"].ArrayValue.ToList();
		list.Add(Value.Int(Mathf.FloorToInt(Now.TotalHours)));
		_keyValueObject["derailments"] = Value.Array(list);
	}

	private List<int> GetDerailmentHoursAndTrim(float expireHours)
	{
		List<int> list = (from v in _keyValueObject["derailments"].ArrayValue
			select v.IntValue into h
			where (float)h > expireHours
			select h).ToList();
		_keyValueObject["derailments"] = Value.Array(list.Select(Value.Int).ToList());
		return list;
	}

	public static List<int> DerailmentHoursToDerailmentsPerDay(List<int> derailmentHours, int days, float hour0)
	{
		List<int> list = new List<int>(days);
		for (int i = 0; i < days; i++)
		{
			list.Add(0);
		}
		foreach (int derailmentHour in derailmentHours)
		{
			int num = Mathf.FloorToInt(((float)derailmentHour - hour0) / 24f);
			if (num >= list.Count)
			{
				Log.Warning("Ignoring out of range derailment hour {hour} (day {day})", derailmentHour, num);
			}
			else
			{
				list[num]++;
			}
		}
		return list;
	}

	public static float CalculateSafetyScoreFromIndividualDayScores(float[] scorePerDay)
	{
		if (scorePerDay.Length == 0)
		{
			throw new ArgumentException("Empty array");
		}
		float a = CalculateEma(scorePerDay, 0.333f);
		float b = scorePerDay.Average();
		return Mathf.Lerp(a, b, 0.5f);
		static float CalculateEma(float[] values, float alpha)
		{
			float num = values[0];
			num = values.Average();
			for (int i = 1; i < values.Length; i++)
			{
				float num2 = values[i];
				num = alpha * num2 + (1f - alpha) * num;
			}
			return num;
		}
	}

	public static string ReputationString(float reputation)
	{
		return $"{Mathf.RoundToInt(reputation * 100f)}%";
	}

	public float RepairBonus()
	{
		float reputation = Reputation;
		if (reputation > 0.8f)
		{
			if (!(reputation > 0.95f))
			{
				if (reputation > 0.9f)
				{
					return 0.25f;
				}
				return 0.1f;
			}
			return 0.5f;
		}
		if (reputation > 0.7f)
		{
			return 0.05f;
		}
		return 0f;
	}

	public float PhaseDiscount()
	{
		float reputation = Reputation;
		if (reputation > 0.8f)
		{
			if (reputation > 0.9f)
			{
				if (reputation > 0.95f)
				{
					return 0.25f;
				}
				return 0.2f;
			}
			if (reputation > 0.85f)
			{
				return 0.15f;
			}
			return 0.1f;
		}
		if (reputation > 0.7f)
		{
			return 0.05f;
		}
		return 0f;
	}

	public float EquipmentDiscount()
	{
		float reputation = Reputation;
		if (reputation > 0.9f)
		{
			if (!(reputation > 0.99f))
			{
				if (reputation > 0.95f)
				{
					return 0.07f;
				}
				return 0.05f;
			}
			return 0.1f;
		}
		if (reputation > 0.8f)
		{
			if (reputation > 0.85f)
			{
				return 0.03f;
			}
			return 0.02f;
		}
		if (reputation > 0.7f)
		{
			return 0.01f;
		}
		return 0f;
	}

	public int ContractMaxStartTier()
	{
		float reputation = Reputation;
		if (!(reputation > 0.95f))
		{
			if (reputation > 0.9f)
			{
				return 2;
			}
			return 1;
		}
		return 3;
	}
}
