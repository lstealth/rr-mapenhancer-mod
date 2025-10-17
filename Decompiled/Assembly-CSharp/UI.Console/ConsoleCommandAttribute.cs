using System;

namespace UI.Console;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class ConsoleCommandAttribute : Attribute
{
	public string CommandName { get; private set; }

	public string Description { get; private set; }

	public ConsoleCommandAttribute(string commandName, string description)
	{
		CommandName = commandName;
		Description = description;
	}
}
