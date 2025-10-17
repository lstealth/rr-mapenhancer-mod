using Core;
using Helpers;
using UnityEngine;

namespace Track;

public static class TrackMath
{
	public static float CalculateCurveDegrees(Graph.PositionRotation a, Graph.PositionRotation b)
	{
		Vector3 vector = a.Position.ZeroY();
		Vector3 vector2 = b.Position.ZeroY();
		float y = a.Rotation.eulerAngles.y;
		float y2 = b.Rotation.eulerAngles.y;
		float num = Mathf.DeltaAngle(y, y2);
		Quaternion quaternion = Quaternion.Euler(0f, y, 0f);
		Quaternion quaternion2 = Quaternion.Euler(0f, y2, 0f);
		if (num < 0f)
		{
			Quaternion quaternion3 = Quaternion.Euler(0f, 180f, 0f);
			quaternion = quaternion3 * quaternion;
			quaternion2 = quaternion3 * quaternion2;
		}
		Vector3 a2 = vector + quaternion * Vector3.right * 1000f;
		Vector3 b2 = vector2 + quaternion2 * Vector3.right * 1000f;
		if (LineSegment.Intersects(vector, a2, vector2, b2, out var point))
		{
			float a3 = Vector3.Distance(vector, point);
			float b3 = Vector3.Distance(vector2, point);
			float num2 = Mathf.Lerp(a3, b3, 0.5f) * 3.28084f;
			return 2f * Mathf.Asin(100f / (2f * num2)) * 57.29578f;
		}
		return 0f;
	}
}
