using System.Collections.Generic;
using KeyValue.Runtime;

namespace Model.Ops;

public struct CarLoadInfo
{
	public string LoadId;

	public float Quantity;

	public Value AsPropertyValue => Value.Dictionary(new Dictionary<string, Value>
	{
		{
			"loadId",
			Value.String(LoadId)
		},
		{
			"quantity",
			Value.Float(Quantity)
		}
	});

	public CarLoadInfo(string loadId, float quantity)
	{
		LoadId = loadId;
		Quantity = quantity;
	}

	public static CarLoadInfo? FromPropertyValue(Value value)
	{
		if (value.Type != ValueType.Dictionary)
		{
			return null;
		}
		IReadOnlyDictionary<string, Value> dictionaryValue = value.DictionaryValue;
		return new CarLoadInfo(dictionaryValue["loadId"].StringValue, dictionaryValue["quantity"].FloatValue);
	}
}
