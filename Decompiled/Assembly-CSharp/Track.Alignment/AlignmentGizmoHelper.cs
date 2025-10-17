using UnityEngine;

namespace Track.Alignment;

public class AlignmentGizmoHelper : MonoBehaviour
{
	[Range(0f, 10f)]
	public float spacing;

	public float length = 200f;

	public Color color = Color.black;

	private void OnDrawGizmos()
	{
		Gizmos.color = color;
		Transform transform = base.transform;
		Vector3 forward = transform.forward;
		Vector3 position = transform.position;
		Gizmos.DrawLine(position + forward * length, position + -forward * length);
		if (spacing > 0.001f)
		{
			Gizmos.color = color * 0.5f;
			for (int i = -3; i <= 3; i++)
			{
				Vector3 vector = transform.right * i * spacing;
				Gizmos.DrawLine(position + vector + forward * length, position + vector + -forward * length);
			}
		}
	}
}
