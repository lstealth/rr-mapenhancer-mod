using System;

namespace Track.Search;

internal struct ClearSwitch
{
	public readonly TrackNode Node;

	public float ClearDistance;

	public override string ToString()
	{
		return $"{Node.id} {ClearDistance:F1}";
	}

	public ClearSwitch(TrackNode node, float clearDistance)
	{
		Node = node;
		ClearDistance = clearDistance;
	}

	public static int Compare(ClearSwitch a, ClearSwitch b)
	{
		return a.ClearDistance.CompareTo(b.ClearDistance);
	}

	public bool Equals(ClearSwitch other)
	{
		if (object.Equals(Node, other.Node))
		{
			return ClearDistance.Equals(other.ClearDistance);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is ClearSwitch other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Node, ClearDistance);
	}
}
