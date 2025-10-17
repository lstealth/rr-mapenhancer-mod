using System.Collections.Generic;
using Game.Messages;
using KeyValue.Runtime;

namespace Model.AI;

public readonly struct Orders
{
	public readonly AutoEngineerMode Mode;

	public readonly bool Forward;

	public readonly int MaxSpeedMph;

	public readonly OrderWaypoint? Waypoint;

	public int SignedMaxSpeedMph => (Forward ? 1 : (-1)) * MaxSpeedMph;

	public bool Enabled => Mode != AutoEngineerMode.Off;

	public static Orders Disabled => new Orders(AutoEngineerMode.Off, forward: true, 0, null);

	public Value PropertyValue
	{
		get
		{
			if (!Enabled)
			{
				return Value.Null();
			}
			return Value.Dictionary(new Dictionary<string, Value>
			{
				{
					"mode",
					(int)Mode
				},
				{
					"forward",
					Value.Bool(Forward)
				},
				{
					"maxSpeedMph",
					Value.Int(MaxSpeedMph)
				},
				{
					"wpt",
					Waypoint?.PropertyValue ?? Value.Null()
				}
			});
		}
	}

	public Orders(AutoEngineerMode mode, bool forward, int maxSpeedMph, OrderWaypoint? waypoint)
	{
		Mode = mode;
		Forward = forward;
		MaxSpeedMph = maxSpeedMph;
		Waypoint = waypoint;
	}

	public Orders WithMaxSpeedMph(int maxSpeedMph)
	{
		return new Orders(Mode, Forward, maxSpeedMph, Waypoint);
	}

	public static Orders? FromPropertyValue(Value value)
	{
		if (value.Type != ValueType.Dictionary)
		{
			return null;
		}
		Value value2 = value["mode"];
		bool boolValue = value["enabled"].BoolValue;
		bool boolValue2 = value["yard"].BoolValue;
		return new Orders((!value2.IsNull) ? ((AutoEngineerMode)value2.IntValue) : (boolValue ? ((!boolValue2) ? AutoEngineerMode.Road : AutoEngineerMode.Yard) : AutoEngineerMode.Off), waypoint: OrderWaypoint.FromPropertyValue(value["wpt"]), forward: value["forward"].BoolValue, maxSpeedMph: value["maxSpeedMph"].IntValue);
	}

	public override string ToString()
	{
		string text = Mode switch
		{
			AutoEngineerMode.Off => "Off", 
			AutoEngineerMode.Road => "Road", 
			AutoEngineerMode.Yard => "Yard", 
			AutoEngineerMode.Waypoint => "Waypoint", 
			_ => Mode.ToString(), 
		};
		return $"mode={text}, forward={Forward}, maxSpeedMph={MaxSpeedMph}, goto={Waypoint}";
	}
}
