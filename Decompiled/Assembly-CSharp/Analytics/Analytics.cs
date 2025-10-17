using System.Collections.Generic;

namespace Analytics;

public static class Analytics
{
	public static void Post(string eventType, Dictionary<string, object> eventProperties = null)
	{
		AnalyticsManager.Shared?.Post(eventType, eventProperties);
	}
}
