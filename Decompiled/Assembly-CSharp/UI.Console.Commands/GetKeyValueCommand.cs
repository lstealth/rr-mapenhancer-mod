using System.Runtime.InteropServices;
using System.Text;
using Game.State;
using KeyValue.Runtime;
using Model;
using UnityEngine;

namespace UI.Console.Commands;

[StructLayout(LayoutKind.Sequential, Size = 1)]
[ConsoleCommand("/get", "Get key value data.")]
public struct GetKeyValueCommand : IConsoleCommand
{
	public string Execute(string[] comps)
	{
		if (comps.Length < 2)
		{
			return "Usage: /get <id> [key]";
		}
		string text = comps[1];
		IKeyValueObject keyValueObject = KeyValueObjectForString(text);
		if (keyValueObject == null)
		{
			return "Object not found.";
		}
		if (comps.Length == 3)
		{
			string text2 = comps[2];
			Value value = keyValueObject[text2];
			StringBuilder stringBuilder = new StringBuilder();
			KeyValueStringHelper.PrintToStringBuilder(value, stringBuilder);
			string text3 = stringBuilder.ToString().Trim();
			Debug.Log("Value of key " + text + "[" + text2 + "]:\n" + text3);
			return text + " " + text2 + " = " + text3;
		}
		string text4 = keyValueObject.ToString().Trim();
		Debug.Log("Value of object " + text + ":\n" + text4);
		return text + " = " + text4;
	}

	public static IKeyValueObject KeyValueObjectForString(string id)
	{
		Car car = TrainController.Shared.CarForString(id);
		if (car != null)
		{
			return car.KeyValueObject;
		}
		return StateManager.Shared.KeyValueObjectForId(id);
	}
}
