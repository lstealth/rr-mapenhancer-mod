using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core;
using GalaSoft.MvvmLight.Messaging;
using Game.AccessControl;
using Game.Events;
using Game.State;
using KeyValue.Runtime;
using Model.Ops;
using Network;
using Serilog;
using UnityEngine;

namespace Game.DailyReport;

public class DailyReportGenerator : GameBehaviour
{
	private KeyValueObject _keyValueObject;

	private Coroutine _tickCoroutine;

	private const string ObjectId = "_dailyReport";

	private const int ReportHourOfDay = 18;

	private static DailyReportGenerator _instance;

	private const string LastGeneratedKey = "lastGenerated";

	private const string ReportKey = "report";

	private static GameDateTime Now => TimeWeather.Now;

	public static DailyReportGenerator Shared
	{
		get
		{
			if (_instance == null)
			{
				_instance = UnityEngine.Object.FindObjectOfType<DailyReportGenerator>();
			}
			return _instance;
		}
	}

	private GameDateTime LastGenerated
	{
		get
		{
			return _keyValueObject["lastGenerated"].GameDateTime(GameDateTime.Zero);
		}
		set
		{
			_keyValueObject["lastGenerated"] = value.KeyValueValue();
		}
	}

	public string LatestReportMarkup
	{
		get
		{
			return _keyValueObject["report"].StringValue;
		}
		private set
		{
			_keyValueObject["report"] = Value.String(value);
		}
	}

	private void Awake()
	{
		KeyValueObject keyValueObject = base.gameObject.AddComponent<KeyValueObject>();
		StateManager.Shared.RegisterPropertyObject("_dailyReport", keyValueObject, AuthorizationRequirement.HostOnly);
		_keyValueObject = keyValueObject;
	}

	private void OnDestroy()
	{
		if (StateManager.Shared != null)
		{
			StateManager.Shared.UnregisterPropertyObject("_dailyReport");
		}
	}

	protected override void OnEnableWithProperties()
	{
		if (StateManager.IsHost)
		{
			_tickCoroutine = StartCoroutine(TickCoroutine());
			Messenger.Default.Register<TimeAdvanced>(this, delegate
			{
				GenerateIfItsTime();
			});
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
		while (true)
		{
			GenerateIfItsTime();
			yield return TimeWeather.WaitForNextHour();
		}
	}

	private void GenerateIfItsTime()
	{
		GameDateTime lastGenerated = LastGenerated;
		GameDateTime now = Now;
		if (now.TimeForDailyEvent(lastGenerated, 18))
		{
			Debug.Log($"Daily Report: Is time {now} vs {lastGenerated}");
			try
			{
				GameDateTime gameDateTime = now.WithHours(18f);
				GenerateReport(gameDateTime);
				LastGenerated = gameDateTime;
				return;
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Error updating reputation");
				return;
			}
		}
		Debug.Log($"Daily Report: Is NOT time {now} vs {lastGenerated}");
	}

	[ContextMenu("Generate Report Now")]
	public void GenerateReportNow()
	{
		GenerateReport(TimeWeather.Now);
	}

	private void GenerateReport(GameDateTime publishTime)
	{
		GameDateTime gameDateTime = publishTime.AddingDays(-1f);
		int startBalance;
		int endBalance;
		IReadOnlyList<Ledger.Entry> ledgerEntries = StateManager.Shared.Ledger.EntriesBetween(gameDateTime, publishTime, out startBalance, out endBalance);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("# Daily Report");
		stringBuilder.AppendLine($"{gameDateTime} to {publishTime}");
		stringBuilder.AppendLine();
		AddWheelReportSection(stringBuilder, ledgerEntries);
		stringBuilder.AppendLine();
		AddFinanceSection(gameDateTime, publishTime, stringBuilder, ledgerEntries, startBalance, endBalance);
		stringBuilder.AppendLine();
		AddInventorySection(stringBuilder);
		stringBuilder.AppendLine();
		AddRepairSection(stringBuilder, publishTime);
		string text = (LatestReportMarkup = stringBuilder.ToString());
		Multiplayer.Broadcast("A new daily report is available.");
		Debug.Log("Daily Report:\n" + text);
	}

	private void AddRepairSection(StringBuilder sb, GameDateTime publishTime)
	{
		OpsController shared = OpsController.Shared;
		sb.AppendLine("## Shops");
		foreach (Industry item in shared.AllIndustries.Where((Industry i) => !i.ProgressionDisabled))
		{
			foreach (RepairTrack item2 in item.VisibleComponents.Where((IndustryComponent ic) => ic is RepairTrack).Cast<RepairTrack>())
			{
				sb.AppendLine("- " + item2.DailyReportSummary(publishTime));
			}
		}
	}

	private void AddInventorySection(StringBuilder sb)
	{
		OpsController shared = OpsController.Shared;
		sb.AppendLine("## Fuel Inventory");
		string[] source = new string[2] { "coal", "diesel-fuel" };
		Industry[] allIndustries = shared.AllIndustries;
		foreach (Industry industry in allIndustries)
		{
			if (industry.ProgressionDisabled)
			{
				continue;
			}
			foreach (IndustryComponent visibleComponent in industry.VisibleComponents)
			{
				if (visibleComponent is IndustryUnloader { orderLoads: false, load: var load } industryUnloader && source.Contains(load.id))
				{
					float quantity = industry.Storage.QuantityInStorage(load);
					string text = load.QuantityString(quantity);
					sb.AppendLine("- " + industryUnloader.DisplayName + ": " + text);
				}
			}
		}
	}

	private static void AddWheelReportSection(StringBuilder sb, IReadOnlyList<Ledger.Entry> ledgerEntries)
	{
		OpsController shared = OpsController.Shared;
		sb.AppendLine("## Operations");
		int number = ledgerEntries.Sum((Ledger.Entry e) => (e.Category == Ledger.Category.Passenger) ? e.Count : 0);
		int number2 = ledgerEntries.Sum((Ledger.Entry e) => (e.Category == Ledger.Category.Freight) ? e.Count : 0);
		sb.AppendLine("- " + number.Pluralize("passenger fare"));
		sb.AppendLine("- " + number2.Pluralize("freight delivery"));
		sb.AppendLine("### Outstanding Waybills");
		foreach (Area area in shared.Areas)
		{
			if (!area.Industries.All((Industry i) => i.ProgressionDisabled))
			{
				int num = shared.CarsInArea(area).Count(delegate(IOpsCar c)
				{
					Waybill? waybill = c.Waybill;
					return waybill.HasValue && !waybill.Value.Completed;
				});
				if (num != 0)
				{
					sb.AppendLine("- " + area.name + ": " + num.Pluralize("car"));
				}
			}
		}
	}

	private static void AddFinanceSection(GameDateTime reportStartTime, GameDateTime publishTime, StringBuilder sb, IReadOnlyList<Ledger.Entry> ledgerEntries, int startBalance, int endBalance)
	{
		sb.AppendLine("## Finance");
		Dictionary<Ledger.Category, int> dictionary = new Dictionary<Ledger.Category, int>();
		foreach (Ledger.Entry ledgerEntry in ledgerEntries)
		{
			if (!dictionary.TryGetValue(ledgerEntry.Category, out var value))
			{
				value = 0;
			}
			value += ledgerEntry.Amount;
			dictionary[ledgerEntry.Category] = value;
		}
		int num = endBalance - startBalance;
		string text = ((num < 0) ? "-" : "+");
		int value2;
		object obj;
		if (num != 0)
		{
			value2 = Mathf.Abs(num);
			obj = text + value2.ToString("$##");
		}
		else
		{
			obj = "no change";
		}
		string arg = (string)obj;
		sb.AppendLine($"Balance: {endBalance:C0} ({arg})");
		foreach (KeyValuePair<Ledger.Category, int> item in dictionary)
		{
			item.Deconstruct(out var key, out value2);
			Ledger.Category category = key;
			int num2 = value2;
			string arg2 = StringForCategory(category);
			sb.AppendLine($"- {num2:C0} {arg2}");
		}
	}

	public static string StringForCategory(Ledger.Category category)
	{
		return category switch
		{
			Ledger.Category.Bank => "Bank", 
			Ledger.Category.Freight => "Freight", 
			Ledger.Category.Passenger => "Passenger", 
			Ledger.Category.Fuel => "Fuel", 
			Ledger.Category.Loan => "Loan", 
			Ledger.Category.Equipment => "Equipment", 
			Ledger.Category.WagesRepair => "Wages: Shop", 
			Ledger.Category.Progression => "Milestones", 
			Ledger.Category.WagesAI => "Wages: Engineer", 
			Ledger.Category.RepairSupplies => "Repair Supplies", 
			_ => throw new ArgumentOutOfRangeException("category", category, null), 
		};
	}

	public IDisposable Observe(Action onChange)
	{
		return _keyValueObject.Observe("report", delegate
		{
			onChange();
		}, callInitial: false);
	}
}
