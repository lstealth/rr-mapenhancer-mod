using System;

namespace Track;

public readonly struct Gauge : IEquatable<Gauge>
{
	public readonly float Inside;

	public readonly float HeadWidth;

	public readonly float RailHeight;

	private readonly int _hashCode;

	public static readonly Gauge Standard = new Gauge(1.435f, 0.076f, 0.177f);

	private Gauge(float inside, float headWidth, float railHeight)
	{
		Inside = inside;
		HeadWidth = headWidth;
		RailHeight = railHeight;
		_hashCode = HashCode.Combine(Inside, HeadWidth, RailHeight);
	}

	public bool Equals(Gauge other)
	{
		float inside = Inside;
		if (inside.Equals(other.Inside))
		{
			inside = HeadWidth;
			if (inside.Equals(other.HeadWidth))
			{
				inside = RailHeight;
				return inside.Equals(other.RailHeight);
			}
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is Gauge other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return _hashCode;
	}
}
