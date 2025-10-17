using System;
using System.Linq;
using Track;
using UnityEngine;

namespace Model.Ops;

public readonly struct OpsCarPosition
{
	public readonly string DisplayName;

	public readonly string Identifier;

	public readonly TrackSpan[] Spans;

	public OpsCarPosition(string displayName, string identifier, TrackSpan[] spans)
	{
		DisplayName = displayName;
		Identifier = identifier;
		Spans = spans;
	}

	public override string ToString()
	{
		return DisplayName + "/" + Identifier;
	}

	public bool Equals(OpsCarPosition other)
	{
		if (object.Equals(Identifier, other.Identifier))
		{
			return object.Equals(Spans, other.Spans);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is OpsCarPosition other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Identifier, Spans);
	}

	public Vector3 GetCenter()
	{
		if (Spans.Length == 0)
		{
			throw new Exception("Position " + Identifier + " has no spans");
		}
		return Spans.Aggregate(Vector3.zero, (Vector3 current, TrackSpan span) => current + span.GetCenterPoint()) / Spans.Length;
	}

	public static explicit operator Location(OpsCarPosition pos)
	{
		Location value = pos.Spans[0].lower.Value;
		value.AssertValid();
		return value;
	}
}
