using System;

namespace InfinityCode.RealWorldTerrain;

public struct RealWorldTerrainTimer
{
	private long start;

	public double seconds => (double)(DateTime.Now.Ticks - start) / 10000000.0;

	public static RealWorldTerrainTimer Start()
	{
		return new RealWorldTerrainTimer
		{
			start = DateTime.Now.Ticks
		};
	}
}
