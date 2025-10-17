using UnityEngine;

namespace Helpers;

public static class GizmoExtensions
{
	public static float GetGizmoSize(Vector3 position)
	{
		Camera current = Camera.current;
		position = Gizmos.matrix.MultiplyPoint(position);
		if ((bool)current)
		{
			Transform transform = current.transform;
			Vector3 position2 = transform.position;
			float z = Vector3.Dot(position - position2, transform.TransformDirection(new Vector3(0f, 0f, 1f)));
			Vector3 vector = current.WorldToScreenPoint(position2 + transform.TransformDirection(new Vector3(0f, 0f, z)));
			Vector3 vector2 = current.WorldToScreenPoint(position2 + transform.TransformDirection(new Vector3(1f, 0f, z)));
			float magnitude = (vector - vector2).magnitude;
			return 80f / Mathf.Max(magnitude, 0.0001f);
		}
		return 20f;
	}
}
