using System.Diagnostics;
using System.IO;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using UnityEngine;

namespace Logging;

public class SerilogUnityConsoleEventSink : ILogEventSink
{
	private readonly ITextFormatter _formatter;

	public SerilogUnityConsoleEventSink(ITextFormatter formatter)
	{
		_formatter = formatter;
	}

	[DebuggerHidden]
	public void Emit(LogEvent logEvent)
	{
		using StringWriter stringWriter = new StringWriter();
		_formatter.Format(logEvent, stringWriter);
		string message = stringWriter.ToString().Trim();
		switch (logEvent.Level)
		{
		case LogEventLevel.Verbose:
		case LogEventLevel.Debug:
		case LogEventLevel.Information:
			UnityEngine.Debug.Log(message);
			break;
		case LogEventLevel.Warning:
			UnityEngine.Debug.LogWarning(message);
			break;
		case LogEventLevel.Error:
		case LogEventLevel.Fatal:
			UnityEngine.Debug.LogError(message);
			break;
		}
	}
}
