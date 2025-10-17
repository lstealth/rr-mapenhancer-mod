using System;
using UnityEngine;

namespace Model.AI;

internal static class ClampUtilities
{
	public static float Clamp(this float input, FloatRange range)
	{
		return Mathf.Clamp(input, range.minimum, range.maximum);
	}

	public static float Scale(this float value, FloatRange range, FloatRange targetRange)
	{
		return Mathf.Lerp(targetRange.minimum, targetRange.maximum, Mathf.InverseLerp(range.minimum, range.maximum, value));
	}

	internal static float ClampToMaximumStep(this float current, float previous, float maximumStep)
	{
		if (maximumStep <= 0f)
		{
			throw new ArgumentOutOfRangeException("Expected maximumStep to be greater than 0");
		}
		if (current - previous > maximumStep)
		{
			return previous + maximumStep;
		}
		if (previous - current > maximumStep)
		{
			return previous - maximumStep;
		}
		return current;
	}
}
