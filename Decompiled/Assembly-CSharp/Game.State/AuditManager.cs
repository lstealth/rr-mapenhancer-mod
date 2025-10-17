using System;
using System.Collections.Generic;
using System.Text;
using Game.AccessControl;
using KeyValue.Runtime;
using Serilog;
using UnityEngine;

namespace Game.State;

public class AuditManager : MonoBehaviour
{
	private struct SwitchEntry
	{
		public readonly int Timestamp;

		public readonly string Action;

		public readonly string Requester;

		public SwitchEntry(int timestamp, string action, string requester)
		{
			Timestamp = timestamp;
			Action = action;
			Requester = requester;
		}

		public static SwitchEntry FromValue(Value value)
		{
			return new SwitchEntry(value["t"], value["a"], value["r"]);
		}

		public Value ToValue()
		{
			return Value.Dictionary(new Dictionary<string, Value>
			{
				{ "t", Timestamp },
				{ "a", Action },
				{ "r", Requester }
			});
		}
	}

	private KeyValueObject _keyValueObject;

	private const string KeyValueIdentifier = "audit";

	public static AuditManager Shared { get; private set; }

	private void Awake()
	{
		Shared = this;
	}

	private void OnEnable()
	{
		_keyValueObject = base.gameObject.AddComponent<KeyValueObject>();
		StateManager.Shared.RegisterPropertyObject("audit", _keyValueObject, AuthorizationRequirement.HostOnly);
	}

	private void OnDisable()
	{
		if (StateManager.Shared != null)
		{
			StateManager.Shared.UnregisterPropertyObject("audit");
		}
	}

	private void OnDestroy()
	{
		Shared = null;
	}

	private static string SwitchKey(string nodeId)
	{
		return "sw:" + nodeId;
	}

	public void RecordSwitchAction(string nodeId, string action, string requester)
	{
		List<SwitchEntry> switchEntries = GetSwitchEntries(nodeId);
		int timestamp = (int)TimeWeather.Now.TotalSeconds;
		switchEntries.Add(new SwitchEntry(timestamp, action, requester));
		while (switchEntries.Count > 3)
		{
			switchEntries.RemoveAt(0);
		}
		SetSwitchEntries(nodeId, switchEntries);
		Log.Information("Audit Switch Action: {nodeId} {requester} \"{action}\"", nodeId, action, GetDisplayRequester(requester));
	}

	public bool TryGetSwitchText(string nodeId, out string report)
	{
		List<SwitchEntry> switchEntries = GetSwitchEntries(nodeId);
		if (switchEntries.Count == 0)
		{
			report = string.Empty;
			return false;
		}
		StringBuilder stringBuilder = new StringBuilder();
		GameDateTime now = TimeWeather.Now;
		switchEntries.Reverse();
		foreach (SwitchEntry item in switchEntries)
		{
			GameDateTime relativeTo = new GameDateTime(item.Timestamp);
			string displayRequester = GetDisplayRequester(item.Requester);
			stringBuilder.AppendLine(now.IntervalString(relativeTo, GameDateTimeInterval.Style.Short) + " ago: " + item.Action + " by " + displayRequester);
		}
		report = stringBuilder.ToString().TrimEnd();
		return true;
	}

	private static string GetDisplayRequester(string requesterString)
	{
		try
		{
			EntityReference r;
			return EntityReference.TryParseURI(requesterString, out r) ? r.Text() : requesterString;
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Exception in GetDisplayRequester");
			return requesterString;
		}
	}

	private void SetSwitchEntries(string nodeId, List<SwitchEntry> entries)
	{
		List<Value> list = new List<Value>(entries.Count);
		foreach (SwitchEntry entry in entries)
		{
			list.Add(entry.ToValue());
		}
		_keyValueObject[SwitchKey(nodeId)] = Value.Array(list);
	}

	private List<SwitchEntry> GetSwitchEntries(string nodeId)
	{
		IReadOnlyList<Value> arrayValue = _keyValueObject[SwitchKey(nodeId)].ArrayValue;
		List<SwitchEntry> list = new List<SwitchEntry>();
		foreach (Value item in arrayValue)
		{
			try
			{
				list.Add(SwitchEntry.FromValue(item));
			}
			catch (Exception exception)
			{
				Log.Warning(exception, "Error parsing switch entry {a}", item);
			}
		}
		return list;
	}
}
