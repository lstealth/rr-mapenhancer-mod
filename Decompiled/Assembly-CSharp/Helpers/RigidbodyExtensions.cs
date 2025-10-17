using UnityEngine;

namespace Helpers;

public static class RigidbodyExtensions
{
	public static void AutoPosition(this Rigidbody rigidbody, Vector3 position, Quaternion rotation, bool immediate = false)
	{
		float magnitude = (rigidbody.position - position).magnitude;
		if (immediate || (magnitude > 50f && rigidbody.velocity.magnitude < 1f))
		{
			rigidbody.position = position;
			rigidbody.rotation = rotation;
			rigidbody.transform.position = position;
			rigidbody.transform.rotation = rotation;
		}
		else
		{
			rigidbody.MovePosition(position);
			rigidbody.MoveRotation(rotation);
		}
	}
}
