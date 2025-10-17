using UnityEngine;

namespace Track.Alignment;

public class PlatformAlignmentGizmo : MonoBehaviour
{
	public float length = 20f;

	public Color color = Color.yellow;

	private void OnDrawGizmos()
	{
		Transform transform = base.transform;
		Vector3 forward = transform.forward;
		Vector3 position = transform.position;
		float num = length / 2f;
		Gizmos.color = color;
		Vector3 vector2;
		Vector3 vector = (vector2 = position + Vector3.up * 1.17f);
		vector2 = vector + transform.right * 2.1f;
		Gizmos.DrawLine(vector2 + forward * num, vector2 + -forward * num);
		vector2 = vector + transform.right * (0f - num);
		Gizmos.DrawLine(vector2 + forward * num, vector2 + -forward * num);
	}
}
