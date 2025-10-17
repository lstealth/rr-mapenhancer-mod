using System.Collections.Generic;
using System.Linq;
using MessagePack;

namespace Game.Messages;

[MessagePackObject(false)]
public struct DictionaryPropertyValue : IPropertyValue
{
	[Key(0)]
	public Dictionary<string, IPropertyValue> Value;

	public DictionaryPropertyValue(Dictionary<string, IPropertyValue> value)
	{
		Value = value;
	}

	public override string ToString()
	{
		return "{" + string.Join(", ", Value.Select((KeyValuePair<string, IPropertyValue> pair) => $"'{pair.Key}': {pair.Value}")) + "}";
	}
}
