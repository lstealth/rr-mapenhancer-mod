using System;

namespace Game.State;

public static class GameModeExtensions
{
	public static string DisplayString(this GameMode gm)
	{
		return gm switch
		{
			GameMode.Company => "Company", 
			GameMode.Sandbox => "Sandbox", 
			_ => throw new ArgumentOutOfRangeException("gm", gm, null), 
		};
	}
}
