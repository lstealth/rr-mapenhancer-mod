using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Serilog;

namespace UI.Console;

public class CommandProcessor
{
	private readonly Dictionary<string, CommandHandlerInfo> _handlers = new Dictionary<string, CommandHandlerInfo>();

	public void RegisterHandlers(Assembly assembly)
	{
		foreach (Type item in from t in assembly.GetTypes()
			where t.GetCustomAttribute<ConsoleCommandHandlerAttribute>() != null
			select t)
		{
			ConsoleCommandHandlerAttribute customAttribute = item.GetCustomAttribute<ConsoleCommandHandlerAttribute>();
			string text = customAttribute.Command.ToLower();
			if (_handlers.ContainsKey(text))
			{
				Log.Error("Handler already registered for {command}: {type}", text, item);
				continue;
			}
			MethodInfo defaultHandler = item.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic).FirstOrDefault((MethodInfo m) => m.GetCustomAttribute<ConsoleDefaultCommandAttribute>() != null);
			CommandHandlerInfo value = new CommandHandlerInfo(item, text, customAttribute.Description, defaultHandler);
			foreach (MethodInfo item2 in from m in item.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic)
				where m.GetCustomAttribute<ConsoleSubcommandAttribute>() != null
				select m)
			{
				ConsoleSubcommandAttribute customAttribute2 = item2.GetCustomAttribute<ConsoleSubcommandAttribute>();
				string text2 = customAttribute2.Name ?? ConsoleSubcommandAttribute.ParseMethodName(item2.Name);
				SubcommandInfo value2 = new SubcommandInfo(text2, customAttribute2.Description, item2);
				value.Subcommands[text2] = value2;
			}
			_handlers[text] = value;
		}
	}

	private string GetUsageString(string command, string subCommand = null)
	{
		if (!_handlers.TryGetValue(command.ToLower(), out var value))
		{
			return "Unknown command '/" + command + "'";
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (string.IsNullOrEmpty(subCommand))
		{
			stringBuilder.AppendLine("/" + command + " <subcommand> ...");
			foreach (SubcommandInfo item in value.Subcommands.Values.OrderBy((SubcommandInfo s) => s.Name))
			{
				stringBuilder.AppendLine("  " + item.GetUsage(command));
			}
		}
		else
		{
			if (!value.Subcommands.TryGetValue(subCommand.ToLower(), out var value2))
			{
				return "Unknown subcommand '/" + command + " " + subCommand + "'";
			}
			stringBuilder.Append(value2.GetUsage(command));
		}
		return stringBuilder.ToString();
	}

	private string GetFriendlyTypeName(Type type)
	{
		if (type == typeof(int))
		{
			return "integer";
		}
		if (type == typeof(float))
		{
			return "float";
		}
		if (type == typeof(string))
		{
			return "string";
		}
		if (type == typeof(bool))
		{
			return "true/false";
		}
		return type.Name.ToLower();
	}

	public bool ProcessCommand(string[] parts, out string output)
	{
		output = null;
		if (parts.Length == 0 || !parts[0].StartsWith("/"))
		{
			return false;
		}
		string text = parts[0].Substring(1).ToLower();
		if (!_handlers.TryGetValue(text, out var value))
		{
			return false;
		}
		output = HandleCommand(parts, text, value);
		return true;
	}

	private string HandleCommand(string[] parts, string command, CommandHandlerInfo handler)
	{
		if (parts.Length < 2)
		{
			if (handler.DefaultHandler != null)
			{
				return InvokeHandlerMethod(parts, command, null, handler, handler.DefaultHandler, 1);
			}
			return "Usage: " + GetUsageString(command);
		}
		string text = parts[1].ToLower();
		if (!handler.Subcommands.TryGetValue(text, out var value))
		{
			if (handler.DefaultHandler != null && handler.DefaultHandler.GetParameters().Length == parts.Length - 1)
			{
				return InvokeHandlerMethod(parts, command, null, handler, handler.DefaultHandler, 1);
			}
			return "Unknown subcommand.\nUsage: " + GetUsageString(command);
		}
		return InvokeHandlerMethod(parts, command, text, handler, value.Method, 2);
	}

	private string InvokeHandlerMethod(string[] parts, string command, string subCommand, CommandHandlerInfo handler, MethodInfo method, int argStart)
	{
		ParameterInfo[] parameters = method.GetParameters();
		object[] array = new object[parameters.Length];
		if (parts.Length - argStart < parameters.Length)
		{
			return $"Expected {parameters.Length} parameters.\nUsage: {GetUsageString(command, subCommand)}";
		}
		for (int i = 0; i < parameters.Length; i++)
		{
			ParameterInfo parameterInfo = parameters[i];
			string text = parts[i + argStart];
			try
			{
				array[i] = Convert.ChangeType(text, parameterInfo.ParameterType);
			}
			catch (Exception)
			{
				string text2 = ((subCommand != null) ? handler.Subcommands[subCommand].GetUsage(command) : GetUsageString(command, subCommand));
				return "Can't interpret '" + text + "' as " + GetFriendlyTypeName(parameterInfo.ParameterType) + " for parameter '" + parameterInfo.Name + "'.\nUsage: " + text2;
			}
		}
		object obj = Activator.CreateInstance(handler.HandlerType);
		return method.Invoke(obj, array) as string;
	}

	public IEnumerable<(string, string)> PublicCommandsAndDescriptions()
	{
		return from h in _handlers
			where !string.IsNullOrEmpty(h.Value.Description)
			select ("/" + h.Value.Command, Description: h.Value.Description);
	}
}
