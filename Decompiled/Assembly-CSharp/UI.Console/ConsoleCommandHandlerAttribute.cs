using System;

namespace UI.Console;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class ConsoleCommandHandlerAttribute : Attribute
{
	public string Command { get; }

	public string Description { get; private set; }

	public ConsoleCommandHandlerAttribute(string command, string description = null)
	{
		Command = command;
		Description = description;
	}
}
