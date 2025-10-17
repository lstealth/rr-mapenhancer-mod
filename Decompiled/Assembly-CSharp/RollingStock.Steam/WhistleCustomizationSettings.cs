using System.Collections.Generic;
using KeyValue.Runtime;

namespace RollingStock.Steam;

public struct WhistleCustomizationSettings
{
	public const string ObjectKey = "whistle.custom";

	private const string KeyIdentifier = "identifier";

	public string WhistleIdentifier { get; }

	public Value PropertyValue => Value.Dictionary(new Dictionary<string, Value> { 
	{
		"identifier",
		string.IsNullOrEmpty(WhistleIdentifier) ? Value.Null() : Value.String(WhistleIdentifier)
	} });

	public WhistleCustomizationSettings(string whistleIdentifier)
	{
		if (string.IsNullOrEmpty(whistleIdentifier) || whistleIdentifier == "a.w.default")
		{
			whistleIdentifier = "wh-5-drg-st";
		}
		WhistleIdentifier = whistleIdentifier;
	}

	public static WhistleCustomizationSettings? FromPropertyValue(Value value)
	{
		if (value.Type != ValueType.Dictionary)
		{
			return null;
		}
		IReadOnlyDictionary<string, Value> dictionaryValue = value.DictionaryValue;
		return new WhistleCustomizationSettings(dictionaryValue.ContainsKey("identifier") ? dictionaryValue["identifier"].StringValue : null);
	}
}
