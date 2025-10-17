using UnityEngine;

namespace Helpers;

public static class QuaternionExtensions
{
	public static bool IsValid(this Quaternion quaternion)
	{
		bool num = float.IsNaN(quaternion.x + quaternion.y + quaternion.z + quaternion.w);
		bool flag = quaternion.x == 0f && quaternion.y == 0f && quaternion.z == 0f && quaternion.w == 0f;
		return !(num || flag);
	}

	public static Quaternion OnlyEulerY(this Quaternion quaternion)
	{
		return Quaternion.Euler(0f, quaternion.eulerAngles.y, 0f);
	}
}
