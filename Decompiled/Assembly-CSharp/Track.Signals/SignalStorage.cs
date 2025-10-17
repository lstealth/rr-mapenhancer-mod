using System;
using System.Collections.Generic;
using System.Linq;
using Game.State;
using KeyValue.Runtime;
using UnityEngine;

namespace Track.Signals;

[RequireComponent(typeof(KeyValueObject))]
public class SignalStorage : MonoBehaviour
{
	private KeyValueObject _keyValueObject;

	private const string KeyUnlockedSwitchIds = "unlockedSwitchIds";

	private readonly Dictionary<string, string> _blockOccupancyKeys = new Dictionary<string, string>();

	private KeyValueObject KeyValueObject => _keyValueObject ?? (_keyValueObject = GetComponent<KeyValueObject>());

	public SystemMode SystemMode
	{
		get
		{
			return (SystemMode)KeyValueObject["mode"].IntValue;
		}
		set
		{
			KeyValueObject["mode"] = Value.Int((int)value);
		}
	}

	private string GetBlockOccupancyKey(string blockId)
	{
		if (_blockOccupancyKeys.TryGetValue(blockId, out var value))
		{
			return value;
		}
		string text = CTCKeys.BlockOccupancy(blockId);
		_blockOccupancyKeys[blockId] = text;
		return text;
	}

	public bool GetBlockOccupied(string blockId)
	{
		return KeyValueObject[GetBlockOccupancyKey(blockId)].BoolValue;
	}

	public void SetBlockOccupied(string blockId, bool value)
	{
		StateManager.AssertIsHost();
		KeyValueObject[GetBlockOccupancyKey(blockId)] = Value.BoolOrNull(value);
	}

	public IDisposable ObserveBlockOccupancy(string blockId, Action<bool> action)
	{
		return ObserveBlockOccupancy(blockId, action, callInitial: true);
	}

	public IDisposable ObserveBlockOccupancy(string blockId, Action<bool> action, bool callInitial)
	{
		return KeyValueObject.Observe(GetBlockOccupancyKey(blockId), delegate(Value value)
		{
			action(value.BoolValue);
		}, callInitial);
	}

	public CTCTrafficFilter GetBlockTrafficFilter(string blockId)
	{
		return (CTCTrafficFilter)KeyValueObject[CTCKeys.BlockTrafficFilter(blockId)].IntValue;
	}

	public void SetBlockTrafficFilter(string blockId, CTCTrafficFilter filter)
	{
		Value value = Value.IntOrNull((int)filter);
		KeyValueObject[CTCKeys.BlockTrafficFilter(blockId)] = value;
	}

	public IDisposable ObserveBlockTrafficFilter(string blockId, Action<CTCTrafficFilter> action)
	{
		return KeyValueObject.Observe(CTCKeys.BlockTrafficFilter(blockId), delegate(Value value)
		{
			action((CTCTrafficFilter)value.IntValue);
		});
	}

	public SignalAspect GetSignalAspect(string signalId)
	{
		return (SignalAspect)KeyValueObject[CTCKeys.SignalAspect(signalId)].IntValue;
	}

	public void SetSignalAspect(string signalId, SignalAspect aspect)
	{
		StateManager.AssertIsHost();
		KeyValueObject[CTCKeys.SignalAspect(signalId)] = Value.Int((int)aspect);
	}

	public void SetSwitchPosition(string switchNodeId, SwitchSetting switchSetting)
	{
		StateManager.AssertIsHost();
		KeyValueObject[CTCKeys.SwitchPosition(switchNodeId)] = Value.IntOrNull((int)switchSetting);
	}

	public IDisposable ObserveSwitchPosition(string switchNodeId, Action<SwitchSetting> action)
	{
		return KeyValueObject.Observe(CTCKeys.SwitchPosition(switchNodeId), delegate(Value value)
		{
			action((SwitchSetting)value.IntValue);
		});
	}

	public IDisposable ObserveSystemMode(Action<SystemMode> action)
	{
		return KeyValueObject.Observe("mode", delegate(Value value)
		{
			action((SystemMode)value.IntValue);
		});
	}

	public IDisposable ObserveSignalAspect(string signalId, Action<SignalAspect> action)
	{
		return KeyValueObject.Observe(CTCKeys.SignalAspect(signalId), delegate(Value value)
		{
			action((SignalAspect)value.IntValue);
		});
	}

	public SignalDirection GetInterlockingDirection(string interlockingId)
	{
		return (SignalDirection)KeyValueObject[CTCKeys.InterlockingDirection(interlockingId)].IntValue;
	}

	public void SetInterlockingDirection(string interlockingId, SignalDirection direction)
	{
		Value value = ((direction == SignalDirection.None) ? Value.Null() : Value.Int((int)direction));
		KeyValueObject[CTCKeys.InterlockingDirection(interlockingId)] = value;
	}

	public IDisposable ObserveInterlockingDirection(string interlockingId, Action<SignalDirection> action)
	{
		return KeyValueObject.Observe(CTCKeys.InterlockingDirection(interlockingId), delegate(Value value)
		{
			action((SignalDirection)value.IntValue);
		});
	}

	public void SetButton(string buttonId, bool value)
	{
		KeyValueObject[CTCKeys.Button(buttonId)] = (value ? Value.Bool(value: true) : Value.Null());
	}

	public IDisposable ObserveButton(string buttonId, Action<bool> action)
	{
		return KeyValueObject.Observe(CTCKeys.Button(buttonId), delegate(Value value)
		{
			action(value.BoolValue);
		});
	}

	public void SetSwitchIdUnlocked(string nodeId, bool unlocked)
	{
		List<Value> list = KeyValueObject["unlockedSwitchIds"].ArrayValue.ToList();
		for (int i = 0; i < list.Count; i++)
		{
			if (!(list[i] != nodeId))
			{
				list.RemoveAt(i);
				i--;
			}
		}
		if (unlocked)
		{
			list.Add(nodeId);
		}
		KeyValueObject["unlockedSwitchIds"] = Value.Array(list);
	}

	public IDisposable ObserveUnlockedSwitchIds(Action<string[]> action)
	{
		return KeyValueObject.Observe("unlockedSwitchIds", delegate(Value value)
		{
			IReadOnlyList<Value> arrayValue = value.ArrayValue;
			string[] array = new string[arrayValue.Count];
			for (int i = 0; i < arrayValue.Count; i++)
			{
				array[i] = arrayValue[i].StringValue;
			}
			action(array);
		});
	}
}
