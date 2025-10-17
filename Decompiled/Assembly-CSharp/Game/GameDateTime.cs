using System;
using UnityEngine;

namespace Game;

public struct GameDateTime
{
	public static readonly GameDateTime Zero;

	private const float HoursToSeconds = 3600f;

	private const float HoursToMinutes = 60f;

	private const float SecondsToHours = 0.00027777778f;

	private const float DaysToSeconds = 86400f;

	private const float SecondsToDays = 1.1574074E-05f;

	private const float SecondsToMinutes = 60f;

	private const float MinutesToHours = 1f / 60f;

	public double TotalSeconds { get; set; }

	public int Day => Mathf.FloorToInt(TotalHours / 24f);

	public float Hours => Mathf.Repeat(TotalHours, 24f);

	public float Minutes => Hours * 60f;

	public float TotalHours => (float)(TotalSeconds * 0.00027777778450399637);

	public float TotalDays => (float)(TotalSeconds * 1.1574074051168282E-05);

	public GameDateTime StartOfDay => WithHours(0f);

	public GameDateTime(int day, float hours)
	{
		TotalSeconds = ((double)day * 24.0 + (double)hours) * 60.0 * 60.0;
	}

	public GameDateTime(double totalSeconds)
	{
		TotalSeconds = totalSeconds;
	}

	public GameDateTime WithHours(float hours)
	{
		return new GameDateTime(Day, hours);
	}

	public GameDateTime WithMinutes(float minutes)
	{
		return WithHours(Mathf.Floor(Hours) + Mathf.Clamp01(minutes / 60f));
	}

	public GameDateTime RoundingHours()
	{
		return WithHours(Mathf.Round(Hours));
	}

	public GameDateTime RoundingMinutes(int minutes)
	{
		if (minutes < 1 || minutes > 30)
		{
			throw new ArgumentException("Out of range", "minutes");
		}
		int num = Mathf.RoundToInt(Hours * 60f / (float)minutes) * minutes;
		return WithHours((float)num * (1f / 60f));
	}

	public override string ToString()
	{
		return DayString() + " " + TimeString();
	}

	public string DayString()
	{
		return $"Day {Day + 1}";
	}

	public string TimeString()
	{
		float hours = Hours;
		int num = Mathf.FloorToInt(hours);
		float num2 = (hours - (float)num) * 60f;
		int num3 = Mathf.RoundToInt(num2);
		int num4 = Mathf.FloorToInt(num2);
		int num5 = (((double)(num2 - (float)num4) > 0.99) ? num3 : num4);
		return $"{num}:{num5:D2}";
	}

	public string ConsoleTimeString()
	{
		float hours = Hours;
		int num = Mathf.FloorToInt(hours);
		int num2 = Mathf.FloorToInt((hours - (float)num) * 60f);
		return $"{num,2}:{num2:D2}";
	}

	public float SecondsSince(GameDateTime other)
	{
		return (float)(TotalSeconds - other.TotalSeconds);
	}

	public float DaysSince(GameDateTime other)
	{
		return SecondsSince(other) * 1.1574074E-05f;
	}

	public readonly GameDateTime AddingSeconds(float seconds)
	{
		return new GameDateTime(TotalSeconds + (double)seconds);
	}

	public GameDateTime AddingMinutes(int minutes)
	{
		return new GameDateTime(TotalSeconds + (double)(minutes * 60));
	}

	public readonly GameDateTime AddingHours(float hours)
	{
		return new GameDateTime(TotalSeconds + (double)(hours * 60f * 60f));
	}

	public readonly GameDateTime AddingDays(float days)
	{
		return new GameDateTime(TotalSeconds + (double)(days * 24f * 60f * 60f));
	}

	public static bool operator <(GameDateTime a, GameDateTime b)
	{
		return a.TotalSeconds < b.TotalSeconds;
	}

	public static bool operator >(GameDateTime a, GameDateTime b)
	{
		return a.TotalSeconds > b.TotalSeconds;
	}

	public static bool operator <=(GameDateTime a, GameDateTime b)
	{
		return a.TotalSeconds <= b.TotalSeconds;
	}

	public static bool operator >=(GameDateTime a, GameDateTime b)
	{
		return a.TotalSeconds >= b.TotalSeconds;
	}

	public static bool operator ==(GameDateTime a, GameDateTime b)
	{
		return Math.Abs(a.TotalSeconds - b.TotalSeconds) < 0.0010000000474974513;
	}

	public static bool operator !=(GameDateTime a, GameDateTime b)
	{
		return !(a == b);
	}

	public static GameDateTime operator +(GameDateTime a, float b)
	{
		return a.AddingSeconds(b);
	}

	public static double operator -(GameDateTime a, GameDateTime b)
	{
		return a.SecondsSince(b);
	}

	private bool Equals(GameDateTime other)
	{
		return TotalSeconds.Equals(other.TotalSeconds);
	}

	public override bool Equals(object obj)
	{
		if (obj is GameDateTime other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return TotalSeconds.GetHashCode();
	}

	public bool TimeForDailyEvent(GameDateTime last, int hourOfDay)
	{
		if (Day <= last.Day)
		{
			return false;
		}
		return Hours >= (float)hourOfDay;
	}
}
