using UnityEngine;

namespace Network;

public static class NetworkTime
{
	public const long AvatarDelay = 300L;

	public const long AvatarIntervalMin = 100L;

	public const long AvatarIntervalMax = 2000L;

	public const long TrainDelay = 300L;

	public const long TrainInterval = 100L;

	public const long TrainMaxInterval = 5000L;

	public const long AirMinInterval = 1000L;

	public static long systemTick => (long)(Time.fixedUnscaledTimeAsDouble * 1000.0);

	public static float Elapsed(long fromTick, long toTick)
	{
		return (float)(toTick - fromTick) / 1000f;
	}
}
