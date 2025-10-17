using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GalaSoft.MvvmLight.Messaging;
using Game;
using HeathenEngineering.SteamworksIntegration.API;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Networking;

namespace Analytics;

public class AnalyticsManager : MonoBehaviour
{
	private Coroutine _coroutine;

	private readonly List<EventPartial> _queue = new List<EventPartial>();

	private Event _template;

	private static readonly DefaultContractResolver ContractResolver = new DefaultContractResolver
	{
		NamingStrategy = new SnakeCaseNamingStrategy()
	};

	private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
	{
		ContractResolver = ContractResolver
	};

	private const string ApiKey = "f2d8c2d26c69b760c5645741cc59c263";

	private static long Timestamp => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

	public static AnalyticsManager Shared { get; private set; }

	private static bool AnalyticsEnabled => Preferences.Analytics == Preferences.AnalyticsPref.OptIn;

	private void Awake()
	{
		Shared = this;
	}

	private void OnEnable()
	{
		_template = new Event(null, SystemInfo.deviceUniqueIdentifier, null, 0L, null, null, null, Timestamp);
		StartStopForPreference();
		Messenger.Default.Register<AnalyticsPreferenceDidChange>(this, delegate
		{
			StartStopForPreference();
		});
	}

	private void OnDisable()
	{
		if (_coroutine != null)
		{
			StopCoroutine(_coroutine);
		}
		_coroutine = null;
		Messenger.Default.Unregister(this);
	}

	private void StartStopForPreference()
	{
		bool analyticsEnabled = AnalyticsEnabled;
		bool flag = _coroutine != null;
		UnityEngine.Analytics.Analytics.enabled = analyticsEnabled;
		if (analyticsEnabled != flag)
		{
			if (analyticsEnabled)
			{
				Log.Information("Analytics: Start");
				_coroutine = StartCoroutine(PostCoroutine());
			}
			else
			{
				Log.Information("Analytics: Stop");
				StopCoroutine(_coroutine);
				_coroutine = null;
			}
		}
	}

	private IEnumerator SendIdentify()
	{
		(string osName, string osVersion) osInfo = GetOsInfo();
		string item = osInfo.osName;
		string item2 = osInfo.osVersion;
		string buildString = GetBuildString();
		Dictionary<string, object> userProperties = new Dictionary<string, object>
		{
			{ "build", buildString },
			{
				"graphicsDeviceName",
				SystemInfo.graphicsDeviceName
			},
			{
				"graphicsDeviceType",
				SystemInfo.graphicsDeviceType.ToString()
			},
			{
				"graphicsDeviceVersion",
				SystemInfo.graphicsDeviceVersion
			},
			{
				"graphicsMemorySize",
				SystemInfo.graphicsMemorySize.ToString()
			},
			{
				"processorType",
				SystemInfo.processorType
			},
			{
				"processorFrequency",
				SystemInfo.processorFrequency.ToString()
			},
			{
				"systemMemorySize",
				SystemInfo.systemMemorySize.ToString()
			}
		};
		string ipCountry = Utilities.Client.IpCountry;
		string value = JsonConvert.SerializeObject(new IdentifyData(null, SystemInfo.deviceUniqueIdentifier, userProperties, Application.version, Application.platform.ToString(), item, item2, ipCountry, null, Application.systemLanguage.ToString()), SerializerSettings);
		UnityWebRequest request = UnityWebRequest.Post("https://api2.amplitude.com/identify", new Dictionary<string, string>
		{
			{ "api_key", "f2d8c2d26c69b760c5645741cc59c263" },
			{ "identification", value }
		});
		yield return request.SendWebRequest();
		if (request.result != UnityWebRequest.Result.Success)
		{
			Log.Error("POST {url} failed: {error}", "https://api2.amplitude.com/identify", request.error);
		}
	}

	private IEnumerator PostCoroutine()
	{
		yield return SendIdentify();
		while (true)
		{
			yield return new WaitForSecondsRealtime(10f);
			if (_queue.Count > 0)
			{
				EventPartial[] events = _queue.ToArray();
				_queue.Clear();
				yield return PostEvents(events);
			}
		}
	}

	public void Post(string eventType, Dictionary<string, object> eventProperties = null)
	{
		if (_coroutine != null)
		{
			long timestamp = Timestamp;
			_queue.Add(new EventPartial(eventType, timestamp, eventProperties));
		}
	}

	private IEnumerator PostEvents(EventPartial[] events)
	{
		Log.Debug("Posting {count} events", events.Length);
		string bodyJson = JsonConvert.SerializeObject(new EventEnvelope
		{
			ApiKey = "f2d8c2d26c69b760c5645741cc59c263",
			Events = events.Select(PartialToFull).ToArray()
		}, SerializerSettings);
		yield return PostJson("https://api2.amplitude.com/2/httpapi", bodyJson);
	}

	private Event PartialToFull(EventPartial e)
	{
		Event template = _template;
		template.EventType = e.EventType;
		template.Time = e.Time;
		template.EventProperties = e.EventProperties;
		return template;
	}

	private IEnumerator PostJson(string url, string bodyJson)
	{
		UnityWebRequest request = new UnityWebRequest(url, "POST");
		byte[] bytes = Encoding.UTF8.GetBytes(bodyJson);
		request.uploadHandler = new UploadHandlerRaw(bytes);
		request.downloadHandler = new DownloadHandlerBuffer();
		request.SetRequestHeader("Content-Type", "application/json");
		yield return request.SendWebRequest();
		if (request.result != UnityWebRequest.Result.Success)
		{
			Log.Error("POST {url} failed: {error} {text}", url, request.error, request.downloadHandler.text);
		}
	}

	private static (string osName, string osVersion) GetOsInfo()
	{
		string operatingSystem = SystemInfo.operatingSystem;
		if (operatingSystem.Contains("Windows"))
		{
			Match match = new Regex("(\\d+(\\.\\d+)+)").Match(operatingSystem);
			if (match.Success)
			{
				string value = match.Groups[1].Value;
				return (osName: "Windows", osVersion: value);
			}
			Log.Error("Failed to parse Windows version string: {str}", operatingSystem);
			return (osName: "Windows", osVersion: "Unknown");
		}
		if (operatingSystem.Contains("Mac OS X"))
		{
			Match match2 = new Regex("Mac OS X (\\d+(\\.\\d+)+)").Match(operatingSystem);
			if (match2.Success)
			{
				string value2 = match2.Groups[1].Value;
				return (osName: "macOS", osVersion: value2);
			}
			Log.Error("Failed to parse Mac version string: {str}", operatingSystem);
			return (osName: "macOS", osVersion: "Unknown");
		}
		Log.Error("Unrecognized operating system: {str}", operatingSystem);
		return (osName: "Unknown", osVersion: "Unknown");
	}

	private static string GetBuildString()
	{
		return "release";
	}
}
