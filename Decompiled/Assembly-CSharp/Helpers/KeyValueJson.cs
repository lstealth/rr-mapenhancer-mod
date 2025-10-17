using System;
using System.Collections.Generic;
using System.IO;
using KeyValue.Runtime;
using Newtonsoft.Json;

namespace Helpers;

internal static class KeyValueJson
{
	public static Value ValueForString(string valueString)
	{
		return ReadValue(new JsonTextReader(new StringReader(valueString)), shouldRead: true);
	}

	private static Value ReadValue(JsonTextReader reader, bool shouldRead)
	{
		if (shouldRead && !reader.Read())
		{
			return Value.Null();
		}
		if (reader.TokenType == JsonToken.Null)
		{
			return Value.Null();
		}
		return reader.TokenType switch
		{
			JsonToken.StartObject => Value.Dictionary(ReadDictionary(reader)), 
			JsonToken.StartArray => Value.Array(ReadArray(reader)), 
			JsonToken.Integer => Value.Int((int)(long)reader.Value), 
			JsonToken.Float => Value.Float((float)(double)reader.Value), 
			JsonToken.String => Value.String((string)reader.Value), 
			JsonToken.Boolean => Value.Bool((bool)reader.Value), 
			_ => throw new Exception($"Unexpected token type {reader.TokenType}"), 
		};
	}

	private static Dictionary<string, Value> ReadDictionary(JsonTextReader reader)
	{
		Dictionary<string, Value> dictionary = new Dictionary<string, Value>();
		while (reader.Read())
		{
			switch (reader.TokenType)
			{
			case JsonToken.PropertyName:
				break;
			case JsonToken.EndObject:
				return dictionary;
			default:
				throw new Exception($"Unexpected token type {reader.TokenType}");
			}
			string key = (string)reader.Value;
			Value value = ReadValue(reader, shouldRead: true);
			dictionary[key] = value;
		}
		throw new Exception("Stream ended");
	}

	private static List<Value> ReadArray(JsonTextReader reader)
	{
		List<Value> list = new List<Value>();
		while (reader.Read())
		{
			if (reader.TokenType == JsonToken.EndArray)
			{
				return list;
			}
			Value item = ReadValue(reader, shouldRead: false);
			list.Add(item);
		}
		throw new Exception("Stream ended");
	}

	public static string StringFromValue(Value propertyValue)
	{
		StringWriter stringWriter = new StringWriter();
		WriteValue(new JsonTextWriter(stringWriter), propertyValue);
		return stringWriter.ToString();
	}

	private static void WriteValue(JsonTextWriter writer, Value value)
	{
		switch (value.Type)
		{
		case KeyValue.Runtime.ValueType.Null:
			writer.WriteNull();
			break;
		case KeyValue.Runtime.ValueType.Int:
			writer.WriteValue(value.IntValue);
			break;
		case KeyValue.Runtime.ValueType.Bool:
			writer.WriteValue(value.BoolValue);
			break;
		case KeyValue.Runtime.ValueType.Float:
			writer.WriteValue(value.FloatValue);
			break;
		case KeyValue.Runtime.ValueType.String:
			writer.WriteValue(value.StringValue);
			break;
		case KeyValue.Runtime.ValueType.Array:
			writer.WriteStartArray();
			foreach (Value item in value.ArrayValue)
			{
				WriteValue(writer, item);
			}
			writer.WriteEndArray();
			break;
		case KeyValue.Runtime.ValueType.Dictionary:
			writer.WriteStartObject();
			foreach (var (name, value3) in value.DictionaryValue)
			{
				writer.WritePropertyName(name);
				WriteValue(writer, value3);
			}
			writer.WriteEndObject();
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}
}
