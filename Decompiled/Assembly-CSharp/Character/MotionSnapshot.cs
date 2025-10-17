using UnityEngine;

namespace Character;

public struct MotionSnapshot
{
	public Vector3 Position;

	public Quaternion BodyRotation;

	public Quaternion LookRotation;

	public Vector3 Velocity;

	public MotionSnapshot(Vector3 position, Quaternion bodyRotation, Quaternion lookRotation, Vector3 velocity)
	{
		Position = position;
		BodyRotation = bodyRotation;
		LookRotation = lookRotation;
		Velocity = velocity;
	}
}
