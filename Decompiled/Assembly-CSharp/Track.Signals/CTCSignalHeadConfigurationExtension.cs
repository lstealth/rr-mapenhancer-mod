using System;

namespace Track.Signals;

public static class CTCSignalHeadConfigurationExtension
{
	public static int IntHeadCount(this SignalHeadConfiguration configuration)
	{
		return configuration switch
		{
			SignalHeadConfiguration.Single => 1, 
			SignalHeadConfiguration.Double => 2, 
			SignalHeadConfiguration.Triple => 3, 
			_ => throw new ArgumentOutOfRangeException("configuration", configuration, null), 
		};
	}
}
