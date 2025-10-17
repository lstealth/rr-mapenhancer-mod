using UnityEngine;

namespace Helpers;

public static class GameObjectExtensions
{
	public static Rigidbody AddKinematicRigidbody(this GameObject target)
	{
		Rigidbody rigidbody = target.AddComponent<Rigidbody>();
		rigidbody.isKinematic = true;
		rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
		rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
		return rigidbody;
	}
}
