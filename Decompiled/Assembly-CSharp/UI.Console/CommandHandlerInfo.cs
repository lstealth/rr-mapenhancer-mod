using System;
using System.Collections.Generic;
using System.Reflection;

namespace UI.Console;

internal readonly struct CommandHandlerInfo
{
	public Type HandlerType { get; }

	public string Command { get; }

	public string Description { get; }

	public MethodInfo DefaultHandler { get; }

	public Dictionary<string, SubcommandInfo> Subcommands { get; }

	public CommandHandlerInfo(Type handlerType, string command, string description, MethodInfo defaultHandler)
	{
		HandlerType = handlerType;
		Command = command.ToLower();
		Description = description;
		DefaultHandler = defaultHandler;
		Subcommands = new Dictionary<string, SubcommandInfo>();
	}
}
