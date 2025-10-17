using System.Collections.Generic;
using UnityEngine;

namespace WorldStreamer2;

public class Vector3ArrayComparerTest : IEqualityComparer<Vector3Int>
{
	public bool Equals(Vector3Int x, Vector3Int y)
	{
		if (x.x == y.x && x.y == y.y && x.z == y.z)
		{
			return true;
		}
		return false;
	}

	public int GetHashCode(Vector3Int obj)
	{
		return ((17 * 23 + obj.x) * 23 + obj.y) * 23 + obj.z;
	}
}
