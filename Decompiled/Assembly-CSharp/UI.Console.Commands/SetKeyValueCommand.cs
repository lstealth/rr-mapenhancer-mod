using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Game.State;
using Helpers;
using KeyValue.Runtime;
using Model;
using Serilog;

namespace UI.Console.Commands;

[StructLayout(LayoutKind.Sequential, Size = 1)]
[ConsoleCommand("/set", "Set key value data.")]
public struct SetKeyValueCommand : IConsoleCommand
{
	public string Execute(string[] comps)
	{
		if (comps.Length < 4)
		{
			return "Usage: /set <id> <key> <value>";
		}
		string text = comps[1];
		string text2 = comps[2];
		Value value = ValueForString(comps[3]);
		if (text == "$config")
		{
			Config config = TrainController.Shared.config;
			object value2 = value.Type switch
			{
				KeyValue.Runtime.ValueType.Null => null, 
				KeyValue.Runtime.ValueType.Int => value.IntValue, 
				KeyValue.Runtime.ValueType.Bool => value.BoolValue, 
				KeyValue.Runtime.ValueType.Float => value.FloatValue, 
				KeyValue.Runtime.ValueType.String => value.StringValue, 
				_ => throw new ArgumentOutOfRangeException(), 
			};
			FieldInfo field = config.GetType().GetField(text2);
			if (field == null)
			{
				throw new Exception("No such property: " + text2);
			}
			field.SetValue(config, value2);
		}
		else
		{
			if (StateManager.Shared.GameMode != GameMode.Sandbox)
			{
				return "Only available in Sandbox.";
			}
			IKeyValueObject keyValueObject = GetKeyValueCommand.KeyValueObjectForString(text);
			if (keyValueObject == null)
			{
				return "Error: '" + text + "' not found.";
			}
			keyValueObject[text2] = value;
		}
		return $"{text}[{text2}] = {value}";
	}

	private Value ValueForString(string valueString)
	{
		try
		{
			return KeyValueJson.ValueForString(valueString);
		}
		catch (Exception exception)
		{
			Log.Information(exception, "Unable to parse, falling back to string: {0}", valueString);
			return Value.String(valueString);
		}
	}
}
