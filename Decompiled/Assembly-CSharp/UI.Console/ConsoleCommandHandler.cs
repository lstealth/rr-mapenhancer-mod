using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Game;
using Game.DailyReport;
using Game.Messages;
using Game.State;
using Helpers;
using Model;
using Serilog;
using UI.Console.Commands;
using UI.Tutorial;
using UnityEngine;

namespace UI.Console;

[RequireComponent(typeof(Console))]
public class ConsoleCommandHandler : MonoBehaviour
{
	private Console _console;

	private readonly Dictionary<string, IConsoleCommand> _commands = new Dictionary<string, IConsoleCommand>();

	private CommandProcessor _processor;

	private void Awake()
	{
		_console = GetComponent<Console>();
		RegisterAllConsoleCommands();
	}

	private void OnEnable()
	{
		_console.OnUserInput -= OnConsoleUserInput;
		_console.OnUserInput += OnConsoleUserInput;
	}

	private void OnDisable()
	{
		_console.OnUserInput -= OnConsoleUserInput;
	}

	private void RegisterAllConsoleCommands()
	{
		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		foreach (Type item in from t in executingAssembly.GetTypes()
			where t.GetCustomAttributes(typeof(ConsoleCommandAttribute), inherit: true).Length != 0
			select t)
		{
			IConsoleCommand command = (IConsoleCommand)Activator.CreateInstance(item);
			Register(command);
		}
		_processor = new CommandProcessor();
		_processor.RegisterHandlers(executingAssembly);
	}

	private void Register<T>(T command) where T : IConsoleCommand
	{
		_commands[command.CommandName()] = command;
	}

	private void OnConsoleUserInput(string line)
	{
		line = line.Trim();
		if (string.IsNullOrWhiteSpace(line))
		{
			return;
		}
		if (line.StartsWith("/"))
		{
			string[] array = Tokenize(line).ToArray();
			if (array.Length != 0)
			{
				string text = HandleSlashCommand(array);
				if (!string.IsNullOrWhiteSpace(text))
				{
					_console.AddLine(text);
				}
			}
		}
		else
		{
			line = line.Truncate(512);
			StateManager.ApplyLocal(new Say(null, line));
		}
	}

	private static List<string> Tokenize(string line)
	{
		List<string> list = new List<string>();
		string text = "";
		for (int i = 0; i < line.Length; i++)
		{
			char c = line[i];
			switch (c)
			{
			case ' ':
				if (text.Length > 0)
				{
					list.Add(text);
					text = "";
				}
				break;
			case '"':
			case '\'':
			{
				char c2 = c;
				for (i++; i < line.Length; i++)
				{
					c = line[i];
					if (c == c2)
					{
						list.Add(text);
						text = "";
						break;
					}
					text += c;
				}
				break;
			}
			default:
				text += c;
				break;
			}
		}
		if (text.Length > 0)
		{
			list.Add(text);
		}
		return list;
	}

	private string HandleSlashCommand(string[] comps)
	{
		try
		{
			return _HandleSlashCommand(comps);
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Error handling slash command");
			Debug.LogException(exception);
			return "Unhandled error.";
		}
	}

	private bool TryGetCommandForSlash(string comp, out IConsoleCommand command)
	{
		if (_commands.TryGetValue(comp, out command))
		{
			return true;
		}
		HashSet<string> hashSet = _commands.Keys.Where((string c) => c.StartsWith(comp)).ToHashSet();
		if (hashSet.Count != 1)
		{
			return false;
		}
		command = _commands[hashSet.First()];
		return true;
	}

	private string _HandleSlashCommand(string[] comps)
	{
		if (_processor.ProcessCommand(comps, out var output))
		{
			return output;
		}
		if (TryGetCommandForSlash(comps[0], out var command))
		{
			return command.Execute(comps);
		}
		switch (comps[0].ToLower())
		{
		case "/help":
			return Help();
		case "/log":
			if (comps.Length == 2)
			{
				if (!(comps[1] == "carsets"))
				{
					return "No such subcommand.";
				}
				TrainController.Shared.LogCarSets();
			}
			return null;
		case "/sysstats":
			return $"System: {SystemInfo.processorType} {SystemInfo.systemMemorySize / 1000}GB\nGraphics: {SystemInfo.graphicsDeviceName} {SystemInfo.graphicsMemorySize:N0}MB";
		case "/time":
		{
			if (comps.Length == 1)
			{
				return TimeWeather.TimeOfDayString;
			}
			if (comps.Length < 2 || !WaitCommand.TryParseHours(comps[1], out var hours))
			{
				return "Usage: /time <hh:mm>";
			}
			StateManager.ApplyLocal(new SetTimeOfDay((float)TimeWeather.Now.WithHours(hours).TotalSeconds));
			return null;
		}
		case "/temult":
			if (comps.Length == 2)
			{
				Car.TractiveForceMultiplier = float.Parse(comps[1]);
			}
			return $"Tractive Effort Multiplier: {Car.TractiveForceMultiplier:F3}";
		case "/tut":
		case "/tutorial":
			TutorialManager.Shared.HandleConsoleCommand(comps[1..].ToArray());
			return null;
		case "/report":
			DailyReportGenerator.Shared.GenerateReportNow();
			return null;
		default:
			return "Command not recognized.";
		}
	}

	private string Help()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		string key;
		foreach (KeyValuePair<string, IConsoleCommand> command in _commands)
		{
			command.Deconstruct(out key, out var value);
			string key2 = key;
			string text = value.CommandDescription();
			if (text != null)
			{
				dictionary[key2] = text;
			}
		}
		foreach (var item3 in _processor.PublicCommandsAndDescriptions())
		{
			string item = item3.Item1;
			string item2 = item3.Item2;
			dictionary[item] = item2;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("Console commands:");
		foreach (KeyValuePair<string, string> item4 in dictionary.OrderBy((KeyValuePair<string, string> kv) => kv.Key))
		{
			item4.Deconstruct(out key, out var value2);
			string text2 = key;
			string text3 = value2;
			stringBuilder.AppendLine(text2 + " - " + text3);
		}
		return stringBuilder.ToString();
	}
}
