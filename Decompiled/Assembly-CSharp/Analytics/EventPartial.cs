using System.Collections.Generic;

namespace Analytics;

internal struct EventPartial
{
	public string EventType;

	public long Time;

	public Dictionary<string, object> EventProperties;

	public EventPartial(string eventType, long time, Dictionary<string, object> eventProperties)
	{
		EventType = eventType;
		Time = time;
		EventProperties = eventProperties;
	}
}
