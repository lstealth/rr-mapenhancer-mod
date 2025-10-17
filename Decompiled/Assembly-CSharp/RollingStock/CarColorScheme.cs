using System.Collections.Generic;
using Helpers;
using KeyValue.Runtime;
using UnityEngine;

namespace RollingStock;

public readonly struct CarColorScheme
{
	public readonly string BaseHex;

	public readonly string DecalHex;

	public readonly Color? Base;

	public readonly Color? Decal;

	public const string ObjectKey = "_colorScheme";

	private const string SubKeyBase = "base";

	private const string SubKeyDecal = "decal";

	public CarColorScheme(string baseHex, string decalHex)
	{
		BaseHex = baseHex;
		DecalHex = decalHex;
		Base = ((BaseHex == null) ? ((Color?)null) : ColorHelper.ColorFromHex(BaseHex));
		Decal = ((DecalHex == null) ? ((Color?)null) : ColorHelper.ColorFromHex(DecalHex));
	}

	public static CarColorScheme From(Value value)
	{
		string stringValue = value["base"].StringValue;
		string stringValue2 = value["decal"].StringValue;
		return new CarColorScheme(stringValue, stringValue2);
	}

	public Value ToValue()
	{
		return Value.Dictionary(new Dictionary<string, Value>
		{
			["base"] = Value.String(BaseHex),
			["decal"] = Value.String(DecalHex)
		});
	}
}
