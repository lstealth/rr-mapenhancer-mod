using System;

namespace Model.Ops.Timetable;

public struct TimetableTime : IEquatable<TimetableTime>
{
	public int Minutes;

	public readonly bool IsAbsolute;

	private TimetableTime(int minutes, bool isAbsolute)
	{
		Minutes = minutes;
		IsAbsolute = isAbsolute;
	}

	public static TimetableTime Relative(int minutes)
	{
		return new TimetableTime(minutes, isAbsolute: false);
	}

	public static TimetableTime Absolute(int minutes)
	{
		return new TimetableTime(minutes % 1440, isAbsolute: true);
	}

	public readonly string TimeString()
	{
		return string.Format("{0}{1}:{2:00}", IsAbsolute ? "" : "+", Minutes / 60, Minutes % 60);
	}

	public override string ToString()
	{
		if (!IsAbsolute)
		{
			return $"+{Minutes}";
		}
		return Minutes.ToString();
	}

	public bool Equals(TimetableTime other)
	{
		if (Minutes == other.Minutes)
		{
			return IsAbsolute == other.IsAbsolute;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is TimetableTime other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Minutes, IsAbsolute);
	}
}
