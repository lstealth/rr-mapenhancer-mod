using System;
using System.Collections.Generic;
using System.Linq;
using KeyValue.Runtime;

namespace Game.Messages;

public static class PropertyValueConverter
{
	public static IPropertyValue RuntimeToSnapshot(Value value)
	{
		return value.Type switch
		{
			KeyValue.Runtime.ValueType.Null => default(NullPropertyValue), 
			KeyValue.Runtime.ValueType.Int => new IntPropertyValue(value.IntValue), 
			KeyValue.Runtime.ValueType.Bool => new BoolPropertyValue(value.BoolValue), 
			KeyValue.Runtime.ValueType.Float => new FloatPropertyValue(value.FloatValue), 
			KeyValue.Runtime.ValueType.String => new StringPropertyValue(value.StringValue), 
			KeyValue.Runtime.ValueType.Array => new ArrayPropertyValue(value.ArrayValue.Select(RuntimeToSnapshot).ToList()), 
			KeyValue.Runtime.ValueType.Dictionary => new DictionaryPropertyValue(RuntimeToSnapshot(value.DictionaryValue)), 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	public static Value SnapshotToRuntime(IPropertyValue value)
	{
		if (value is NullPropertyValue)
		{
			_ = (NullPropertyValue)(object)value;
			return Value.Null();
		}
		if (!(value is BoolPropertyValue boolPropertyValue))
		{
			if (!(value is FloatPropertyValue floatPropertyValue))
			{
				if (!(value is IntPropertyValue intPropertyValue))
				{
					if (!(value is StringPropertyValue stringPropertyValue))
					{
						if (!(value is ArrayPropertyValue arrayPropertyValue))
						{
							if (value is DictionaryPropertyValue dictionaryPropertyValue)
							{
								return Value.Dictionary(SnapshotToRuntime(dictionaryPropertyValue.Value));
							}
							throw new InvalidOperationException();
						}
						return Value.Array(arrayPropertyValue.Value.Select(SnapshotToRuntime).ToList());
					}
					return Value.String(stringPropertyValue.Value);
				}
				return Value.Int(intPropertyValue.Value);
			}
			return Value.Float(floatPropertyValue.Value);
		}
		return Value.Bool(boolPropertyValue.Value);
	}

	public static Value ToRuntime(this IPropertyValue value)
	{
		return SnapshotToRuntime(value);
	}

	public static Value ToRuntime(this Dictionary<string, IPropertyValue> dictionary)
	{
		return SnapshotToRuntime(new DictionaryPropertyValue(dictionary));
	}

	public static Dictionary<string, IPropertyValue> RuntimeToSnapshot(IReadOnlyDictionary<string, Value> dictionary)
	{
		return new Dictionary<string, IPropertyValue>(dictionary.Select((KeyValuePair<string, Value> kv) => new KeyValuePair<string, IPropertyValue>(kv.Key, RuntimeToSnapshot(kv.Value))));
	}

	public static Dictionary<string, Value> SnapshotToRuntime(IReadOnlyDictionary<string, IPropertyValue> dictionary)
	{
		if (dictionary == null)
		{
			return null;
		}
		return new Dictionary<string, Value>(dictionary.Select((KeyValuePair<string, IPropertyValue> kv) => new KeyValuePair<string, Value>(kv.Key, SnapshotToRuntime(kv.Value))));
	}

	public static Dictionary<string, IPropertyValue> SnapshotValues(this IKeyValueObject obj)
	{
		Dictionary<string, IPropertyValue> dictionary = new Dictionary<string, IPropertyValue>();
		foreach (string key in obj.Keys)
		{
			Value value = obj[key];
			dictionary[key] = RuntimeToSnapshot(value);
		}
		return dictionary;
	}

	public static void ApplyValues(this IKeyValueObject obj, Dictionary<string, Value> values)
	{
		foreach (KeyValuePair<string, Value> value3 in values)
		{
			value3.Deconstruct(out var key, out var value);
			string key2 = key;
			Value value2 = value;
			obj[key2] = value2;
		}
	}
}
