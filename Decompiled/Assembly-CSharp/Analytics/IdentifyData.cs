using System.Collections.Generic;

namespace Analytics;

internal struct IdentifyData
{
	public string UserId;

	public string DeviceId;

	public Dictionary<string, object> UserProperties;

	public string AppVersion;

	public string Platform;

	public string OsName;

	public string OsVersion;

	public string Country;

	public string Region;

	public string Language;

	public IdentifyData(string userId, string deviceId, Dictionary<string, object> userProperties, string appVersion, string platform, string osName, string osVersion, string country, string region, string language)
	{
		UserId = userId;
		DeviceId = deviceId;
		UserProperties = userProperties;
		AppVersion = appVersion;
		Platform = platform;
		OsName = osName;
		OsVersion = osVersion;
		Country = country;
		Region = region;
		Language = language;
	}
}
