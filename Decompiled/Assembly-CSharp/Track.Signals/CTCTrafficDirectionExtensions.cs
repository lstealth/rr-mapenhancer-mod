using System;

namespace Track.Signals;

internal static class CTCTrafficDirectionExtensions
{
	public static bool Matches(this CTCTrafficFilter filter, SignalDirection direction)
	{
		return filter switch
		{
			CTCTrafficFilter.None => direction == SignalDirection.None, 
			CTCTrafficFilter.Right => direction == SignalDirection.Right, 
			CTCTrafficFilter.Left => direction == SignalDirection.Left, 
			CTCTrafficFilter.Any => true, 
			_ => throw new ArgumentOutOfRangeException("filter", filter, null), 
		};
	}

	public static bool PreventsSettingRoute(this CTCTrafficFilter filter, SignalDirection direction)
	{
		return filter switch
		{
			CTCTrafficFilter.None => false, 
			CTCTrafficFilter.Right => direction == SignalDirection.Left, 
			CTCTrafficFilter.Left => direction == SignalDirection.Right, 
			CTCTrafficFilter.Any => false, 
			_ => throw new ArgumentOutOfRangeException("filter", filter, null), 
		};
	}

	public static CTCTrafficFilter AsFilter(this SignalDirection direction)
	{
		return direction switch
		{
			SignalDirection.None => CTCTrafficFilter.None, 
			SignalDirection.Right => CTCTrafficFilter.Right, 
			SignalDirection.Left => CTCTrafficFilter.Left, 
			_ => throw new ArgumentOutOfRangeException("direction", direction, null), 
		};
	}
}
