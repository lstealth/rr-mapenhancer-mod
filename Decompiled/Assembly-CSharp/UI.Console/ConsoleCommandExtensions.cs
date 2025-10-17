using System;

namespace UI.Console;

public static class ConsoleCommandExtensions
{
	public static string CommandName(this IConsoleCommand cmd)
	{
		return ((ConsoleCommandAttribute)Attribute.GetCustomAttribute(cmd.GetType(), typeof(ConsoleCommandAttribute))).CommandName;
	}

	public static string CommandDescription(this IConsoleCommand cmd)
	{
		return ((ConsoleCommandAttribute)Attribute.GetCustomAttribute(cmd.GetType(), typeof(ConsoleCommandAttribute))).Description;
	}
}
