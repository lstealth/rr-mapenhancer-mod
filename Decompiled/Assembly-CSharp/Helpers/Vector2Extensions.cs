using UnityEngine;

namespace Helpers;

public static class Vector2Extensions
{
	public static Vector2 Round(this Vector2 v)
	{
		return new Vector2(Mathf.Round(v.x), Mathf.Round(v.y));
	}
}
