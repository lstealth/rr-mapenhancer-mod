using System;
using System.Collections.Generic;
using KeyValue.Runtime;
using MoonSharp.Interpreter;

namespace Game.Scripting;

public class ScriptProperties
{
	private readonly IKeyValueObject _keyValueObject;

	private readonly Script _script;

	public DynValue this[string key]
	{
		get
		{
			return FromValue(_keyValueObject[key], _script);
		}
		set
		{
			_keyValueObject[key] = ToValue(value);
		}
	}

	internal ScriptProperties(IKeyValueObject keyValueObject, Script script = null)
	{
		_keyValueObject = keyValueObject;
		_script = script ?? ScriptManager.CurrentScript;
	}

	public DynValue observe(string key, Closure luaCallback, bool callInitial = true)
	{
		Action<Value> action = delegate(Value value)
		{
			DynValue dynValue = FromValue(value, _script);
			luaCallback.Call(dynValue);
		};
		return UserData.Create(new ScriptDisposable(_keyValueObject.Observe(key, action, callInitial)));
	}

	public static DynValue FromValue(Value value, Script script = null)
	{
		if (script == null)
		{
			script = ScriptManager.CurrentScript;
		}
		return value.Type switch
		{
			KeyValue.Runtime.ValueType.Null => DynValue.Nil, 
			KeyValue.Runtime.ValueType.Int => DynValue.NewNumber(value.IntValue), 
			KeyValue.Runtime.ValueType.Bool => value.BoolValue ? DynValue.True : DynValue.False, 
			KeyValue.Runtime.ValueType.Float => DynValue.NewNumber(value.FloatValue), 
			KeyValue.Runtime.ValueType.String => DynValue.NewString(value.StringValue), 
			KeyValue.Runtime.ValueType.Array => throw new NotImplementedException(), 
			KeyValue.Runtime.ValueType.Dictionary => CreateTable(value.DictionaryValue, script), 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	private static DynValue CreateTable(IReadOnlyDictionary<string, Value> dictionary, Script script)
	{
		Table table = new Table(script);
		foreach (KeyValuePair<string, Value> item in dictionary)
		{
			item.Deconstruct(out var key, out var value);
			string key2 = key;
			Value value2 = value;
			table[key2] = FromValue(value2, script);
		}
		return DynValue.NewTable(table);
	}

	public static Value ToValue(DynValue dynValue)
	{
		return dynValue.Type switch
		{
			DataType.Nil => Value.Null(), 
			DataType.Void => Value.Null(), 
			DataType.Boolean => Value.Bool(dynValue.Boolean), 
			DataType.Number => DynValueNumber(dynValue.Number), 
			DataType.String => dynValue.String, 
			DataType.Table => throw new NotImplementedException(), 
			DataType.Tuple => throw new NotImplementedException(), 
			DataType.Function => throw new NotSupportedException($"type {dynValue.Type} not supported"), 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	private static Value DynValueNumber(double dynValue)
	{
		if (dynValue % 1.0 == 0.0)
		{
			return Value.Int((int)dynValue);
		}
		return Value.Float((float)dynValue);
	}
}
