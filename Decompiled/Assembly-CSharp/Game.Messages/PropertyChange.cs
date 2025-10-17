using System;
using System.Collections.Generic;
using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[PropertyChangeAuthorizationRule]
[MessagePackObject(false)]
public struct PropertyChange : IGameMessage
{
	public enum Control
	{
		Throttle,
		Reverser,
		LocomotiveBrake,
		TrainBrake,
		Horn,
		Bell,
		Handbrake,
		Bleed,
		Compressor,
		CutOut,
		Idle,
		Headlight,
		BrakeStyle,
		Condition,
		Derailment,
		Mu,
		CylinderCock,
		Hotbox
	}

	[Key(0)]
	public string ObjectId;

	[Key(1)]
	public string Key;

	[Key(2)]
	public IPropertyValue Value;

	private static readonly Dictionary<Control, string> KeyMapping = new Dictionary<Control, string>
	{
		{
			Control.Throttle,
			"throttle"
		},
		{
			Control.Reverser,
			"reverser"
		},
		{
			Control.LocomotiveBrake,
			"locoBrake"
		},
		{
			Control.TrainBrake,
			"trainBrake"
		},
		{
			Control.Horn,
			"horn"
		},
		{
			Control.Bell,
			"bell"
		},
		{
			Control.Handbrake,
			"handbrake"
		},
		{
			Control.Bleed,
			"bleed"
		},
		{
			Control.Compressor,
			"compressor"
		},
		{
			Control.CutOut,
			"cutOut"
		},
		{
			Control.Idle,
			"idle"
		},
		{
			Control.Headlight,
			"headlight"
		},
		{
			Control.BrakeStyle,
			"brakeStyle"
		},
		{
			Control.Condition,
			"_condition"
		},
		{
			Control.Derailment,
			"_derailment"
		},
		{
			Control.Mu,
			"mu"
		},
		{
			Control.CylinderCock,
			"cylCock"
		},
		{
			Control.Hotbox,
			"hotbox"
		}
	};

	public PropertyChange(string objectId, string key, IPropertyValue value)
	{
		ObjectId = objectId;
		Key = key;
		Value = value;
	}

	public PropertyChange(string carId, Control control, float value)
	{
		ObjectId = carId;
		Key = KeyForControl(control);
		Value = new FloatPropertyValue(value);
	}

	public PropertyChange(string carId, Control control, int value)
	{
		ObjectId = carId;
		Key = KeyForControl(control);
		Value = new IntPropertyValue(value);
	}

	public PropertyChange(string carId, Control control, bool value)
	{
		ObjectId = carId;
		Key = KeyForControl(control);
		Value = new BoolPropertyValue(value);
	}

	public override string ToString()
	{
		return $"PropertyChange({ObjectId}[{Key}] = {Value})";
	}

	public static string KeyForControl(Control control)
	{
		if (KeyMapping.TryGetValue(control, out var value))
		{
			return value;
		}
		throw new ArgumentOutOfRangeException("control", control, null);
	}

	public static Control ControlForKey(string key)
	{
		foreach (var (result, text2) in KeyMapping)
		{
			if (text2 == key)
			{
				return result;
			}
		}
		throw new ArgumentOutOfRangeException("key", key, null);
	}
}
