using System;
using System.Text.RegularExpressions;

namespace UI.Console;

[AttributeUsage(AttributeTargets.Method)]
public class ConsoleSubcommandAttribute : Attribute
{
	public string Name { get; }

	public string Description { get; }

	public ConsoleSubcommandAttribute(string name = null, string description = null)
	{
		Name = name?.ToLower();
		Description = description;
	}

	public static string ParseMethodName(string methodName)
	{
		return Regex.Replace(methodName, "(?<!^)(?=[A-Z])", "-").ToLower();
	}
}
