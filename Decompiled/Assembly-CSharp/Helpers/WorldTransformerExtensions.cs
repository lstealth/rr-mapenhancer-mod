using UnityEngine;

namespace Helpers;

public static class WorldTransformerExtensions
{
	public static Vector3 WorldToGame(this Vector3 worldPosition)
	{
		return WorldTransformer.WorldToGame(worldPosition);
	}

	public static Vector3 GameToWorld(this Vector3 gamePosition)
	{
		return WorldTransformer.GameToWorld(gamePosition);
	}
}
