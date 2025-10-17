using System;
using Game.Messages;

namespace Model.AI;

public static class AutoEngineerOrdersExtensions
{
	public static int MaxSpeedMph(this AutoEngineerMode mode)
	{
		return mode switch
		{
			AutoEngineerMode.Off => 0, 
			AutoEngineerMode.Road => 45, 
			AutoEngineerMode.Waypoint => 45, 
			AutoEngineerMode.Yard => 15, 
			_ => throw new ArgumentOutOfRangeException("mode", mode, null), 
		};
	}
}
