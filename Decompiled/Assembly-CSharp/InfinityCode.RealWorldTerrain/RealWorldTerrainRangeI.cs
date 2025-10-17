using System;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain;

[Serializable]
public class RealWorldTerrainRangeI
{
	public int min = 1;

	public int minLimit = int.MinValue;

	public int max = 50;

	public int maxLimit = int.MaxValue;

	public RealWorldTerrainRangeI()
	{
	}

	public RealWorldTerrainRangeI(int min, int max, int minLimit = int.MinValue, int maxLimit = int.MaxValue)
	{
		this.min = min;
		this.max = max;
		this.minLimit = minLimit;
		this.maxLimit = maxLimit;
	}

	public void Set(float min, float max)
	{
		this.min = Mathf.Max(minLimit, (int)min);
		this.max = Mathf.Min(maxLimit, (int)max);
	}

	public int Random()
	{
		return UnityEngine.Random.Range(min, max);
	}
}
