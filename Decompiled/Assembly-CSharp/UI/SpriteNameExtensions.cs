using Serilog;
using UnityEngine;

namespace UI;

public static class SpriteNameExtensions
{
	public static Sprite Sprite(this SpriteName spriteName)
	{
		SpriteLibrary shared = SpriteLibrary.Shared;
		if (shared == null)
		{
			Log.Warning("No SpriteLibrary available");
			return null;
		}
		foreach (SpriteLibrary.Entry entry in shared.entries)
		{
			if (entry.name == spriteName)
			{
				return entry.sprite;
			}
		}
		Log.Warning("No sprite in library {spriteName}", spriteName);
		return null;
	}
}
