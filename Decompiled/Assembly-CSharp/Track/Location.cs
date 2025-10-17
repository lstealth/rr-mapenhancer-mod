using System;
using UnityEngine;

namespace Track;

public readonly struct Location : IEquatable<Location>
{
	public readonly TrackSegment segment;

	public readonly float distance;

	public readonly TrackSegment.End end;

	public float Distance => distance;

	public bool IsValid
	{
		get
		{
			if (segment != null && distance >= 0f)
			{
				return distance <= segment.GetLength();
			}
			return false;
		}
	}

	public bool EndIsA => end == TrackSegment.End.A;

	public static Location Invalid => new Location(null, 0f, TrackSegment.End.A);

	public string NodeString
	{
		get
		{
			if (EndIsA)
			{
				return $"{segment.a.id}_{segment.b.id}_{distance:F1}";
			}
			return $"{segment.b.id}_{segment.a.id}_{distance:F1}";
		}
	}

	public Location(TrackSegment segment, float distance, TrackSegment.End end)
	{
		this.segment = segment;
		this.distance = distance;
		this.end = end;
	}

	public Location(Location other)
	{
		segment = other.segment;
		distance = other.distance;
		end = other.end;
	}

	public Location Clamped(out float remainder)
	{
		if (segment == null)
		{
			throw new InvalidLocationException("null segment");
		}
		if (distance <= 0f)
		{
			remainder = distance;
			return new Location(segment, 0f, end);
		}
		float length = segment.GetLength();
		if (distance > length)
		{
			remainder = distance - length;
			return new Location(segment, length, end);
		}
		remainder = 0f;
		return this;
	}

	public Vector3 GetPosition(PositionAccuracy accuracy = PositionAccuracy.Standard)
	{
		segment.GetPositionRotationAtDistance(distance, end, accuracy, out var position, out var _);
		return position;
	}

	public Quaternion GetRotation(PositionAccuracy accuracy = PositionAccuracy.Standard)
	{
		segment.GetPositionRotationAtDistance(distance, end, accuracy, out var _, out var rotation);
		return rotation;
	}

	public Location Clamped()
	{
		float length = segment.GetLength();
		return new Location(segment, Mathf.Clamp(distance, 0f, length), end);
	}

	public float DistanceTo(TrackNode node)
	{
		if (segment.a == node)
		{
			if (!EndIsA)
			{
				return segment.GetLength() - distance;
			}
			return distance;
		}
		if (segment.b == node)
		{
			if (!EndIsA)
			{
				return distance;
			}
			return segment.GetLength() - distance;
		}
		throw new Exception("node is not on segment");
	}

	public float DistanceTo(TrackSegment.End anEnd)
	{
		return anEnd switch
		{
			TrackSegment.End.A => EndIsA ? distance : (segment.GetLength() - distance), 
			TrackSegment.End.B => EndIsA ? (segment.GetLength() - distance) : distance, 
			_ => throw new ArgumentOutOfRangeException("anEnd", anEnd, null), 
		};
	}

	public float DistanceUntilEnd()
	{
		return DistanceTo(EndIsA ? segment.b : segment.a);
	}

	public Location Flipped()
	{
		return new Location(segment, segment.GetLength() - distance, EndIsA ? TrackSegment.End.B : TrackSegment.End.A);
	}

	public Location Moving(float d)
	{
		return new Location(segment, distance + d, end);
	}

	public void AssertValid()
	{
		if (segment == null)
		{
			throw new InvalidLocationException("null segment");
		}
		if (distance < 0f)
		{
			float num = distance;
			throw new InvalidLocationException("distance " + num + " < 0");
		}
		if (distance > segment.GetLength())
		{
			float num = distance;
			throw new InvalidLocationException("distance " + num + " > " + segment.GetLength());
		}
	}

	public override string ToString()
	{
		return string.Format("<{0}, {1} -> {2:F3}>", segment?.id ?? "<nil>", end, distance);
	}

	public bool Equals(Location other, float tolerance)
	{
		if ((object)segment == other.segment && Mathf.Abs(distance - other.distance) < tolerance)
		{
			return end == other.end;
		}
		return false;
	}

	public bool Equals(Location other)
	{
		return Equals(other, 1E-06f);
	}

	public override bool Equals(object obj)
	{
		if (obj is Location other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		int num = ((segment != null) ? segment.GetHashCode() : 0) * 397;
		float num2 = distance;
		return ((num ^ num2.GetHashCode()) * 397) ^ (end == TrackSegment.End.A).GetHashCode();
	}

	public static bool operator ==(Location a, Location b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(Location x, Location y)
	{
		return !(x == y);
	}

	public SerializableLocation Serializable()
	{
		return new SerializableLocation
		{
			segmentId = segment.id,
			distance = distance,
			end = end
		};
	}

	public Location WithEnd(TrackSegment.End desiredEnd)
	{
		if (end != desiredEnd)
		{
			return Flipped();
		}
		return this;
	}

	public static bool TryMatchSegment(Location loc, TrackSegment segment, out Location matched)
	{
		matched = loc;
		TrackSegment trackSegment = loc.segment;
		if (trackSegment == segment)
		{
			return true;
		}
		float locSegmentLength = trackSegment.GetLength();
		float segmentLength = segment.GetLength();
		if (trackSegment.a == segment.a)
		{
			return TryMatch(TrackSegment.End.A, TrackSegment.End.A, out matched);
		}
		if (trackSegment.a == segment.b)
		{
			return TryMatch(TrackSegment.End.A, TrackSegment.End.B, out matched);
		}
		if (trackSegment.b == segment.a)
		{
			return TryMatch(TrackSegment.End.B, TrackSegment.End.A, out matched);
		}
		if (trackSegment.b == segment.b)
		{
			return TryMatch(TrackSegment.End.B, TrackSegment.End.B, out matched);
		}
		return false;
		static bool CloseTo(float a, float b)
		{
			return Mathf.Abs(a - b) < 0.001f;
		}
		bool TryMatch(TrackSegment.End locEnd, TrackSegment.End segmentEnd, out Location result)
		{
			if (loc.end == locEnd)
			{
				if (CloseTo(loc.distance, 0f))
				{
					result = new Location(segment, segmentLength, segmentEnd.Flipped());
					return true;
				}
			}
			else if (CloseTo(loc.distance, locSegmentLength))
			{
				result = new Location(segment, 0f, segmentEnd);
				return true;
			}
			result = loc;
			return false;
		}
	}
}
