using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game;
using Game.State;
using KeyValue.Runtime;
using Model.Ops.Definition;
using Network;
using Serilog;
using UnityEngine;

namespace Model.Ops;

public class Industry : GameBehaviour, IProgressionDisablable
{
	public string identifier;

	[Tooltip("True if this industry governs its behavior by contract.")]
	public bool usesContract;

	private IndustryComponent[] _cachedComponents;

	private IKeyValueObject _keyValueObject;

	private Coroutine _tickCoroutine;

	private const string ContractKey = "contract";

	private const string NextContractKey = "nextContract";

	private const string KeyReceivedCarCount = "_recvdCars";

	private const string PerformanceHistoryKey = "_perfHist";

	private const int PerformanceHistoryLimit = 7;

	public bool ProgressionDisabled { get; set; }

	public IEnumerable<IndustryComponent> Components
	{
		get
		{
			if (_cachedComponents == null)
			{
				_cachedComponents = GetComponentsInChildren<IndustryComponent>();
			}
			return _cachedComponents;
		}
	}

	public IEnumerable<IIndustryTrackDisplayable> TrackDisplayables => from td in GetComponentsInChildren<IIndustryTrackDisplayable>()
		where td.IsVisible
		select td;

	public IEnumerable<IndustryComponent> VisibleComponents => from c in Components
		where c.IsVisible
		orderby c.DisplayName
		select c;

	public IEnumerable<IndustryComponent> UniqueVisibleComponents => from c in Components
		where c.IsVisible
		group c by c.DisplayName into g
		select g.First() into c
		orderby c.DisplayName
		select c;

	public IndustryStorageHelper Storage { get; private set; }

	public EntityReference EntityReference => new EntityReference(EntityType.Industry, identifier);

	internal IKeyValueObject KeyValueObject => _keyValueObject;

	public Contract? Contract
	{
		get
		{
			return Model.Ops.Contract.FromPropertyValue(_keyValueObject["contract"]);
		}
		internal set
		{
			_keyValueObject["contract"] = value?.PropertyValue ?? Value.Null();
		}
	}

	public Contract? NextContract
	{
		get
		{
			return Model.Ops.Contract.FromPropertyValue(_keyValueObject["nextContract"]);
		}
		private set
		{
			_keyValueObject["nextContract"] = value?.PropertyValue ?? Value.Null();
		}
	}

	public IReadOnlyDictionary<int, float> PerformanceHistory
	{
		get
		{
			return _keyValueObject["_perfHist"].DictionaryValue.Select((KeyValuePair<string, Value> v) => new KeyValuePair<int, float>(int.Parse(v.Key), v.Value.FloatValue)).ToDictionary((KeyValuePair<int, float> pair) => pair.Key, (KeyValuePair<int, float> pair) => pair.Value);
		}
		private set
		{
			_keyValueObject["_perfHist"] = Value.Dictionary(value.ToDictionary((KeyValuePair<int, float> pair) => pair.Key.ToString(), (KeyValuePair<int, float> pair) => Value.Float(pair.Value)));
		}
	}

	internal int ReceivedCarCount
	{
		get
		{
			return _keyValueObject["_recvdCars"].IntValue;
		}
		set
		{
			_keyValueObject["_recvdCars"] = ((value <= 0) ? Value.Null() : Value.Int(value));
		}
	}

	public bool IncludeInFreightPerformance(GameDateTime now)
	{
		if (!ProgressionDisabled)
		{
			return this.HasActiveContract(now);
		}
		return false;
	}

	private void Awake()
	{
		_keyValueObject = base.gameObject.AddComponent<KeyValueObject>();
		Storage = new IndustryStorageHelper(_keyValueObject, identifier);
	}

	private void OnDestroy()
	{
		Storage?.Dispose();
	}

	protected override void OnEnableWithProperties()
	{
		if (StateManager.IsHost)
		{
			_tickCoroutine = StartCoroutine(TickCoroutine());
		}
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		if (_tickCoroutine != null)
		{
			StopCoroutine(_tickCoroutine);
			_tickCoroutine = null;
		}
	}

	internal void MockSetKeyValueObject(IKeyValueObject keyValueObject)
	{
		_keyValueObject = keyValueObject;
	}

	[ContextMenu("Simulate Counts")]
	public void SimulateCounts()
	{
		for (int i = 1; i <= 5; i++)
		{
			CarCounter.Count(this, i, 100);
		}
	}

	private IEnumerator TickCoroutine()
	{
		yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 15f));
		InitializeIfNeeded();
		int tickCount = 0;
		while (true)
		{
			yield return new WaitForSeconds(5f);
			bool shouldService = tickCount % 3 == 0;
			Tick(15f, shouldService);
			tickCount++;
		}
	}

	internal void InitializeIfNeeded()
	{
		string version = Application.version;
		string stringValue = _keyValueObject["init"].StringValue;
		if (stringValue == version)
		{
			return;
		}
		GameVersion fromVersion = GameVersion.FromStringOrZero(stringValue);
		Log.Debug("Initializing industry {identifier}", identifier);
		foreach (var (industryComponent, industryContext) in EnumerateComponentContexts(0f))
		{
			industryComponent.Initialize(industryContext, fromVersion);
		}
		_keyValueObject["init"] = version;
	}

	private void Tick(float serviceInterval, bool shouldService)
	{
		if (ProgressionDisabled)
		{
			return;
		}
		foreach (var (industryComponent, industryContext) in EnumerateComponentContexts(serviceInterval))
		{
			try
			{
				industryComponent.CheckForCompleted(industryContext);
				if (shouldService && !industryComponent.ProgressionDisabled)
				{
					industryComponent.Service(industryContext);
				}
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Exception during tick {industry} {component}", this, industryComponent);
			}
		}
	}

	public IEnumerable<(IndustryComponent, IndustryContext)> EnumerateComponentContexts(float dt)
	{
		GameDateTime now = TimeWeather.Now;
		foreach (IndustryComponent component in Components)
		{
			IndustryContext item = component.CreateContext(now, dt);
			yield return (component, item);
		}
	}

	public static void TickAll(float dt)
	{
		Industry[] array = UnityEngine.Object.FindObjectsOfType<Industry>();
		float a = GameTimeHoursToDeltaTime(4f);
		float num = dt;
		while (num > 0f)
		{
			float num2 = Mathf.Min(a, num);
			num -= num2;
			Industry[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].Tick(num2, shouldService: true);
			}
		}
	}

	public static float GameTimeHoursToDeltaTime(float hours)
	{
		return hours * 60f * 60f;
	}

	public bool Contains(OpsCarPosition position)
	{
		foreach (IndustryComponent component in Components)
		{
			if (position.Equals(component))
			{
				return true;
			}
		}
		return false;
	}

	public void OrderCars()
	{
		foreach (var (industryComponent, industryContext) in EnumerateComponentContexts(1f))
		{
			if (!industryComponent.ProgressionDisabled)
			{
				industryComponent.OrderCars(industryContext);
			}
		}
	}

	public IDisposable ObserveContract(Action<Contract?> closure, bool callInitial = true)
	{
		return _keyValueObject.Observe("contract", delegate
		{
			closure(Contract);
		}, callInitial);
	}

	public IDisposable ObserveNextContract(Action<Contract?> closure, bool callInitial = true)
	{
		return _keyValueObject.Observe("nextContract", delegate
		{
			closure(NextContract);
		}, callInitial);
	}

	public void SetContract(Contract? contract)
	{
		Contract = contract;
		ClearPerformanceHistory();
	}

	private float? CalculatePerformance(IReadOnlyList<float> waybillAgesInDays, GameDateTime now)
	{
		waybillAgesInDays = waybillAgesInDays.Where((float age) => age > 0.1f).ToList();
		float num;
		if (waybillAgesInDays.Count == 0)
		{
			num = 1f;
		}
		else
		{
			float value = waybillAgesInDays.Average();
			num = Mathf.InverseLerp(5f, 1f, value);
		}
		if (num > 0.99f && ReceivedCarCount < 1)
		{
			Log.Information("CalculatePerformance: {identifier}, {ages} -> null - not enough cars received", identifier, waybillAgesInDays);
			return null;
		}
		Log.Information("CalculatePerformance: {identifier}, {ages} -> {performance}", identifier, waybillAgesInDays, num);
		return num;
	}

	public void UpdatePerformance(IReadOnlyList<float> waybillAgesInDays, GameDateTime now)
	{
		float? num = CalculatePerformance(waybillAgesInDays, now);
		if (num.HasValue)
		{
			AddPerformanceHistoryEntry(now.Day, num.Value);
		}
	}

	private void AddPerformanceHistoryEntry(int day, float performance)
	{
		StateManager.DebugAssertIsHost();
		Dictionary<int, float> dictionary = new Dictionary<int, float>(PerformanceHistory);
		if (dictionary.ContainsKey(day))
		{
			Log.Warning("History already contains entry for day {day} -- ignoring ({identifier})", day, identifier);
			return;
		}
		dictionary[day] = performance;
		while (dictionary.Count > 7)
		{
			dictionary.Remove(dictionary.Keys.Min());
		}
		PerformanceHistory = dictionary;
	}

	private void ClearPerformanceHistory()
	{
		StateManager.DebugAssertIsHost();
		_keyValueObject["_perfHist"] = Value.Null();
		ReceivedCarCount = 0;
	}

	public void ApplyToBalance(int total, Ledger.Category category, string memo = null, int count = 0, bool quiet = false)
	{
		StateManager.Shared.ApplyToBalance(total, category, EntityReference, memo, count, quiet);
	}

	public void ModifyContract(int modifyTier)
	{
		StateManager.AssertIsHost();
		if (modifyTier == 0)
		{
			if (Contract.HasValue)
			{
				NextContract = new Contract(0);
			}
			else
			{
				NextContract = null;
			}
			return;
		}
		if (modifyTier == Contract?.Tier)
		{
			NextContract = null;
			return;
		}
		foreach (Contract item in this.AvailableContracts())
		{
			if (item.Tier == modifyTier)
			{
				NextContract = item;
				break;
			}
		}
	}

	public void DailyReceivables(GameDateTime now)
	{
		foreach (var (industryComponent, industryContext) in EnumerateComponentContexts(0f))
		{
			industryComponent.DailyReceivables(now, industryContext);
		}
	}

	public void DailyPayables(GameDateTime now)
	{
		foreach (var (industryComponent, industryContext) in EnumerateComponentContexts(0f))
		{
			industryComponent.DailyPayables(now, industryContext);
		}
	}

	private static bool IsFailing(IReadOnlyDictionary<int, float> performanceHistory, int currentTier, out int proposedTier)
	{
		proposedTier = currentTier;
		List<float> list = (from kv in performanceHistory
			orderby kv.Key descending
			select kv.Value).Take(3).ToList();
		if (list.Count < 3)
		{
			return false;
		}
		if (list.Average() >= 0.5f)
		{
			return false;
		}
		proposedTier = Mathf.Max(0, currentTier - 2);
		return true;
	}

	public void RollToNextContract()
	{
		Contract? contract = Contract;
		Contract? contract2 = NextContract;
		IReadOnlyDictionary<int, float> performanceHistory = PerformanceHistory;
		string text = "";
		if (contract.HasValue && IsFailing(performanceHistory, contract.Value.Tier, out var proposedTier))
		{
			Log.Information("Failing contract: {history}, proposed {proposed}", performanceHistory, proposedTier);
			if ((contract2?.Tier ?? contract.Value.Tier) > proposedTier)
			{
				text = " (Low performance.)";
				contract2 = new Contract(proposedTier);
			}
		}
		if (contract2.HasValue)
		{
			Log.Information("RollToNextContract: {tier}", contract2.Value.Tier);
			int count = performanceHistory.Count;
			int tierChangeComponent;
			int ageComponent;
			int num = this.PenaltyForChange(contract2.Value.Tier, count, out tierChangeComponent, out ageComponent);
			string text2 = "";
			if (num > 0)
			{
				Log.Information($"Penalty {num:C} from {count} days");
				StateManager.Shared.ApplyToBalance(-num, Ledger.Category.Freight, EntityReference, "Tier Change Penalty", 0, quiet: true);
				text2 = $" ({num:C0} Penalty)";
			}
			string text3;
			if (contract2.Value.Tier == 0)
			{
				SetContract(null);
				text3 = "has been terminated";
				OpsController.Shared.ReturnWaybillsFrom(this);
			}
			else
			{
				SetContract(contract2);
				text3 = $"is now at Tier {contract2.Value.Tier}";
			}
			Multiplayer.Broadcast($"Contract with {Hyperlink.To(this)} {text3}.{text}{text2}");
			NextContract = null;
		}
	}

	public bool TryGetStorageCapacity(Load load, out float capacity)
	{
		foreach (IndustryComponent component in Components)
		{
			if (!(component is IndustryLoaderBase industryLoaderBase))
			{
				if (component is IndustryUnloader industryUnloader && industryUnloader.load == load)
				{
					capacity = industryUnloader.maxStorage * this.GetContractMultiplier();
					return true;
				}
			}
			else if (industryLoaderBase.load == load)
			{
				capacity = industryLoaderBase.maxStorage;
				return true;
			}
		}
		capacity = 0f;
		return false;
	}
}
