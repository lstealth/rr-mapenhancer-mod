using UnityEngine;

namespace Helpers;

public static class Vector3Extensions
{
	public static Vector3 ZeroY(this Vector3 vector)
	{
		vector.y = 0f;
		return vector;
	}

	public static bool IsZero(this Vector3 vector)
	{
		return (double)vector.sqrMagnitude < 9.99999943962493E-11;
	}

	public static Vector2 XY(this Vector3 v3)
	{
		return new Vector2(v3.x, v3.y);
	}

	public static Vector2 XZ(this Vector3 v3)
	{
		return new Vector2(v3.x, v3.z);
	}
}
