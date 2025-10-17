using System;
using Serilog;
using Serilog.Configuration;
using Serilog.Formatting.Display;

namespace Logging;

public static class SerilogUnityConsoleExtensions
{
	private const string DefaultDebugOutputTemplate = "[{Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}";

	public static LoggerConfiguration UnityConsole(this LoggerSinkConfiguration sinkConfiguration, string outputTemplate = "[{Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}", IFormatProvider formatProvider = null)
	{
		MessageTemplateTextFormatter formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);
		return sinkConfiguration.Sink(new SerilogUnityConsoleEventSink(formatter));
	}
}
