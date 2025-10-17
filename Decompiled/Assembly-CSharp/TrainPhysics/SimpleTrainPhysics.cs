using System;
using UnityEngine;

namespace TrainPhysics;

public class SimpleTrainPhysics
{
	private static readonly int[] throttleToForce = new int[9] { 0, 25000, 62000, 125000, 200000, 300000, 400000, 600000, 800000 };

	public static float TractiveForce(int throttle)
	{
		float num = ((throttle >= 0) ? 1 : (-1));
		int num2 = Math.Abs(throttle);
		return num * (float)throttleToForce[num2];
	}

	public static float CalculateVelocity(float velocity, float mass, float Ftraction, float dt)
	{
		if ((double)Mathf.Abs(velocity) < 0.001 && Mathf.Abs(Ftraction) < 0.001f)
		{
			return velocity;
		}
		if ((double)mass < 0.001)
		{
			Debug.LogWarning("0 mass!");
		}
		float num = 1.29f;
		float num2 = 14.2584f;
		float num3 = 100f;
		float num4 = 0.5f * num3 * num2 * num;
		float num5 = num4 * 30f;
		float num6 = (0f - num4) * velocity * Mathf.Abs(velocity);
		float num7 = (0f - num5) * velocity;
		float num8 = (Ftraction + num6 + num7) / mass * dt;
		float f = velocity;
		velocity += num8;
		if (Mathf.Abs(f) > Mathf.Abs(velocity) && Mathf.Abs(velocity) < 0.1f)
		{
			velocity = 0f;
		}
		return velocity;
	}
}
