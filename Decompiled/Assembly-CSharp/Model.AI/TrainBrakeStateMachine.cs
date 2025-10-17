using UnityEngine;

namespace Model.AI;

public class TrainBrakeStateMachine
{
	internal AnimationCurve ApplyTimeForNumberOfCars;

	internal AnimationCurve ReleaseTimeForNumberOfCars;

	internal AnimationCurve BrakeSetForDeltaVelocityMph;

	internal AnimationCurve BrakeSetMultiplierForVelocityMph;

	public int NumberOfCars = 10;

	public float ApplyTimeout;

	public float ReleaseTimeout;

	internal float VelocityAfterApply;

	internal float VelocityAfterRelease;

	internal float ApplyVelocityThreshold;

	internal float ReleaseVelocityThreshold;

	public float Update(float targetVelocityAbs, float currentVelocityAbs, float currentAccel, float currentSet, float dt, float gradeAhead)
	{
		float num = ApplyTimeForNumberOfCars.Evaluate(NumberOfCars);
		float num2 = ReleaseTimeForNumberOfCars.Evaluate(NumberOfCars);
		float num3 = currentVelocityAbs + currentAccel * num * 0.5f;
		float num4 = currentVelocityAbs + currentAccel * num2 * 0.5f;
		ApplyTimeout -= dt;
		ReleaseTimeout -= dt;
		float num5 = ((targetVelocityAbs < 0.044703927f) ? 0f : (targetVelocityAbs + 0.44703928f));
		float num6 = Mathf.Max(targetVelocityAbs * 0.8f, targetVelocityAbs - 1.3411179f);
		ApplyVelocityThreshold = num5;
		ReleaseVelocityThreshold = num6;
		VelocityAfterApply = num3;
		VelocityAfterRelease = num4;
		if (num3 > num5)
		{
			if (ApplyTimeout > 0f)
			{
				return 0f;
			}
			float num7 = ((targetVelocityAbs < 0.044703927f) ? (targetVelocityAbs - 0.44703928f) : targetVelocityAbs);
			float num8 = num3 - num7;
			if (num8 < 0.44703928f)
			{
				return 0f;
			}
			ApplyTimeout = num * 0.9f;
			ReleaseTimeout = num;
			float num9 = BrakeSetForDeltaVelocityMph.Evaluate(num8 * 2.23694f) * BrakeSetMultiplierForVelocityMph.Evaluate(currentVelocityAbs * 2.23694f);
			num9 *= Mathf.Max(0f, gradeAhead * 0.5f) + 1f;
			Debug.Log($"TBSM: {num9:F1} lb set, timeout = {num:F1}s ({num3 * 2.23694f:F1} > {num5 * 2.23694f:F1} (target {targetVelocityAbs * 2.23694f:F1}))");
			return Mathf.Clamp(num9, 0f, 26f);
		}
		if (num4 < num6)
		{
			if (ReleaseTimeout > 0f)
			{
				return 0f;
			}
			if (currentSet > 0.1f)
			{
				Debug.Log($"TBSM: release {num4 * 2.23694f:F1} < {num6 * 2.23694f:F1} ({targetVelocityAbs * 2.23694f:F1})");
			}
			ApplyTimeout = 0f;
			ReleaseTimeout = 0f;
			return -1f;
		}
		return 0f;
	}

	public void Reset()
	{
		ApplyTimeout = 0f;
		ReleaseTimeout = 0f;
	}
}
