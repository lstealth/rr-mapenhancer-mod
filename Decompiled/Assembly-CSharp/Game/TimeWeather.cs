using System;
using System.Collections;
using System.Collections.Generic;
using Enviro;
using Serilog;
using UnityEngine;

namespace Game;

public static class TimeWeather
{
	private struct TimeState
	{
		public float UnityTime { get; set; }

		public GameDateTime EpochGameDateTime { get; set; }

		public float TimeMultiplier { get; set; }

		public TimeState(float unityTime, GameDateTime epochGameDateTime, float timeMultiplier)
		{
			UnityTime = unityTime;
			EpochGameDateTime = epochGameDateTime;
			TimeMultiplier = timeMultiplier;
		}

		public GameDateTime GameDateTimeForTime(float unityTime)
		{
			float num = (unityTime - UnityTime) / 3600f;
			float timeMultiplier = TimeMultiplier;
			float num2 = EpochGameDateTime.TotalHours + num * timeMultiplier;
			float num3 = Mathf.Repeat(num2, 24f);
			return new GameDateTime(Mathf.RoundToInt((num2 - num3) / 24f), num3);
		}
	}

	private static TimeState _timeSate;

	public static GameDateTime Now
	{
		get
		{
			float time = Time.time;
			return _timeSate.GameDateTimeForTime(time);
		}
		set
		{
			_timeSate.UnityTime = Time.time;
			_timeSate.EpochGameDateTime = value;
		}
	}

	public static float TimeMultiplier
	{
		get
		{
			return _timeSate.TimeMultiplier;
		}
		set
		{
			MarkTime();
			_timeSate.TimeMultiplier = value;
		}
	}

	public static DateTime StartDateTime => new DateTime(1940, 4, 1);

	private static EnviroManager Enviro => EnviroManager.instance;

	public static float SunLevel
	{
		get
		{
			if (Enviro == null)
			{
				return 1f;
			}
			return Mathf.InverseLerp(0.3f, 0.5f, Enviro.solarTime);
		}
	}

	public static string TimeOfDayString => Now.ToString();

	public static int WeatherId
	{
		get
		{
			if (Enviro == null)
			{
				return 0;
			}
			EnviroWeatherType preset = Enviro.Weather.targetWeatherType;
			int num = Enviro.Weather.Settings.weatherTypes.FindIndex((EnviroWeatherType p) => p == preset);
			if (num < 0)
			{
				num = 0;
			}
			return num;
		}
		set
		{
			if (Enviro == null)
			{
				Log.Error("Can't set weather to {weatherIndex} -- no Enviro reference", value);
				return;
			}
			List<EnviroWeatherType> weatherTypes = Enviro.Weather.Settings.weatherTypes;
			if (value < 0 || value >= weatherTypes.Count)
			{
				Log.Error("Can't set weather to {weatherIndex} -- out of range 0..<{count}", value, weatherTypes.Count);
				return;
			}
			EnviroWeatherType type = weatherTypes[value];
			Enviro.Weather.ChangeWeather(type);
		}
	}

	public static Dictionary<string, int> WeatherIdLookup => new Dictionary<string, int>
	{
		["clear"] = 0,
		["cloudy1"] = 1,
		["cloudy2"] = 2,
		["fog"] = 3,
		["rain"] = 4,
		["cloudy3"] = 6
	};

	public static void Reset()
	{
		Now = GameDateTime.Zero;
	}

	public static void MarkTime()
	{
		Now = Now;
	}

	public static IEnumerator WaitForNextDay()
	{
		int startDay = Now.Day;
		while (true)
		{
			int day = Now.Day;
			if (startDay != day)
			{
				break;
			}
			yield return new WaitForSeconds(5f);
		}
	}

	public static IEnumerator WaitForNextHour()
	{
		int startHour = (int)Now.Hours;
		while (true)
		{
			int num = (int)Now.Hours;
			if (startHour != num)
			{
				break;
			}
			yield return new WaitForSeconds(5f);
		}
	}
}
