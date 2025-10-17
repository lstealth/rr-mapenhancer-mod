using System;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain;

[Serializable]
public struct RealWorldTerrainVector2i
{
	public int x;

	public int y;

	public static RealWorldTerrainVector2i one => new RealWorldTerrainVector2i(1, 1);

	public int count => x * y;

	public int max => Mathf.Max(x, y);

	public RealWorldTerrainVector2i(int X = 0, int Y = 0)
	{
		x = X;
		y = Y;
	}

	public override string ToString()
	{
		return $"X: {x}, Y: {y}";
	}

	public static implicit operator Vector2(RealWorldTerrainVector2i val)
	{
		return new Vector2(val.x, val.y);
	}

	public static implicit operator int(RealWorldTerrainVector2i val)
	{
		return val.count;
	}

	public static RealWorldTerrainVector2i operator +(RealWorldTerrainVector2i v1, RealWorldTerrainVector2i v2)
	{
		return new RealWorldTerrainVector2i(v1.x + v2.x, v1.y + v2.y);
	}

	public static RealWorldTerrainVector2i operator -(RealWorldTerrainVector2i v1, RealWorldTerrainVector2i v2)
	{
		return new RealWorldTerrainVector2i(v1.x - v2.x, v1.y - v2.y);
	}
}
