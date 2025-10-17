using UnityEngine;

namespace Helpers;

public static class AnimationCurveExtensions
{
	public static float FindTimeForValue(this AnimationCurve curve, float value, float tolerance, int maxIterations = 10)
	{
		float num = curve.keys[0].time;
		float num2 = curve.keys[0].value;
		float num3 = curve.keys[^1].time;
		float num4 = curve.keys[^1].value;
		if (value <= num2)
		{
			return num;
		}
		if (value >= num4)
		{
			return num3;
		}
		float num5 = Mathf.Lerp(num, num3, Mathf.InverseLerp(num2, num4, value));
		float num6 = curve.Evaluate(num5);
		int num7 = 0;
		do
		{
			if (value < num6)
			{
				num3 = num5;
				num4 = num6;
			}
			else
			{
				num = num5;
				num2 = num6;
			}
			num5 = Mathf.Lerp(num, num3, Mathf.InverseLerp(num2, num4, value));
			num6 = curve.Evaluate(num5);
			num7++;
		}
		while (Mathf.Abs(num6 - value) > tolerance && num7 < maxIterations);
		return num5;
	}
}
