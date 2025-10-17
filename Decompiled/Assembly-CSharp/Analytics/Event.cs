using System.Collections.Generic;

namespace Analytics;

internal struct Event
{
	public string UserId;

	public string DeviceId;

	public string EventType;

	public long Time;

	public Dictionary<string, object> EventProperties;

	public Dictionary<string, object> UserProperties;

	public string Ip;

	public long SessionId;

	public Event(string userId, string deviceId, string eventType, long time, Dictionary<string, object> eventProperties, Dictionary<string, object> userProperties, string ip, long sessionId)
	{
		UserId = userId;
		DeviceId = deviceId;
		EventType = eventType;
		Time = time;
		EventProperties = eventProperties;
		UserProperties = userProperties;
		Ip = ip;
		SessionId = sessionId;
	}
}
