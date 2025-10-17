using System.Collections.Generic;
using KeyValue.Runtime;

namespace Avatar;

public struct AccessoryDescriptor
{
	public string Identifier;

	public string Option;

	public AccessoryDescriptor(string identifier, string option)
	{
		Identifier = identifier;
		Option = option;
	}

	public static AccessoryDescriptor From(Value value)
	{
		IReadOnlyDictionary<string, Value> dictionaryValue = value.DictionaryValue;
		return new AccessoryDescriptor(dictionaryValue["id"].StringValue, dictionaryValue["option"].StringValue);
	}

	public Value ToValue()
	{
		return Value.Dictionary(new Dictionary<string, Value>
		{
			["id"] = Value.String(Identifier),
			["option"] = Value.String(Option)
		});
	}
}
