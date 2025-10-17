using System;
using UnityEngine;

namespace Model.Physics;

public static class TrainMath
{
	public readonly struct SteamEngineCharacteristics
	{
		public readonly int NumberOfCylinders;

		public readonly float PistonDiameter;

		public readonly float PistonStroke;

		public readonly float DriverDiameter;

		public readonly float TotalHeatingSurface;

		private readonly float? _overrideStartingTractiveEffort;

		public SteamEngineCharacteristics(int numberOfCylinders, float pistonDiameter, float pistonStroke, float driverDiameter, float totalHeatingSurface, float? overrideStartingTractiveEffort)
		{
			NumberOfCylinders = numberOfCylinders;
			PistonDiameter = pistonDiameter;
			PistonStroke = pistonStroke;
			DriverDiameter = driverDiameter;
			TotalHeatingSurface = totalHeatingSurface;
			_overrideStartingTractiveEffort = overrideStartingTractiveEffort;
		}

		public float StartingTractiveEffort(float boilerPressurePsi)
		{
			if (_overrideStartingTractiveEffort.HasValue)
			{
				return _overrideStartingTractiveEffort.Value;
			}
			return 0.85f * Mathf.Pow(PistonDiameter, 2f) * (float)NumberOfCylinders * PistonStroke * boilerPressurePsi / (2f * DriverDiameter);
		}
	}

	public enum TrackCondition
	{
		Dry,
		Wet,
		Slick
	}

	private static readonly float[] teRatio06 = new float[4] { 0.999f, 3.58f, 40.6f, 0.26f };

	private static readonly float[] teRatio14 = new float[4] { 0.999f, 3.11f, 21.82f, 0.17f };

	private static readonly float[] teRatio18 = new float[4] { 0.999f, 2.63f, 15.54f, 0.12f };

	public const float LbsToKgs = 0.453592f;

	public const float LbsToNewtons = 4.44822f;

	public const float MinimumCurveSpeedMph = 5f;

	private const float DamageOver = 1.3411179f;

	private const float DerailmentOver = 2.6822357f;

	public static float TractiveEffort(SteamEngineCharacteristics chars, float velocityMph, float boilerPressure)
	{
		float num = chars.StartingTractiveEffort(boilerPressure);
		if (velocityMph < 0.001f)
		{
			return num;
		}
		return num * TractiveEffortFalloff(num, chars.TotalHeatingSurface, velocityMph);
	}

	private static float TractiveEffortFalloff(float startingTractiveEffort, float totalHeatingSurface, float velocityMph)
	{
		if (totalHeatingSurface == 0f)
		{
			totalHeatingSurface = 1500f;
		}
		float num = startingTractiveEffort / totalHeatingSurface;
		if (num < 14f)
		{
			float t = Mathf.InverseLerp(6f, 14f, num);
			return Mathf.Lerp(CalculateCurve(velocityMph, teRatio06), CalculateCurve(velocityMph, teRatio14), t);
		}
		float t2 = Mathf.InverseLerp(14f, 18f, num);
		return Mathf.Lerp(CalculateCurve(velocityMph, teRatio14), CalculateCurve(velocityMph, teRatio18), t2);
	}

	private static float CalculateCurve(float x, float[] vars)
	{
		return CalculateCurve(x, vars[0], vars[1], vars[2], vars[3]);
	}

	private static float CalculateCurve(float x, float a, float b, float c, float d)
	{
		return d + (a - d) / (1f + Mathf.Pow(x / c, b));
	}

	public static float TrackCoefficientOfFriction(TrackCondition condition, float wheelVelocityMps)
	{
		float num = Mathf.Abs(wheelVelocityMps * 3.6f);
		return condition switch
		{
			TrackCondition.Dry => 7.5f / (num + 44f) + 0.161f, 
			TrackCondition.Wet => 7.5f / (num + 44f) + 0.13f, 
			TrackCondition.Slick => 0.05f, 
			_ => throw new ArgumentOutOfRangeException("condition", condition, null), 
		};
	}

	public static float ReverserPowerMultiplier(float absReverser, float absVelocityMph, float maxSpeedMph)
	{
		if (absReverser < 0.1f)
		{
			return 0f;
		}
		float num = maxSpeedMph * (1f - Mathf.Sqrt(Mathf.Clamp01(Mathf.InverseLerp(0.1f, 1f, absReverser))));
		float num2 = Sigmoid(absReverser, 500f, -0.05f);
		float num3 = 1f;
		float num4 = Sigmoid(absVelocityMph, 0.2f, 45f - num);
		float num5 = Sigmoid(0f - absVelocityMph, 0.6f, num + 11f);
		if (absReverser > 0.9f && absVelocityMph < 1f)
		{
			float t = Mathf.Lerp(Mathf.InverseLerp(0f, 1f, absVelocityMph), Mathf.InverseLerp(1f, 0.9f, absReverser), 0.5f);
			num4 = Mathf.Lerp(1f, num4, t);
			num5 = Mathf.Lerp(1f, num5, t);
		}
		return (num4 * num5 * 1f + 0f) * num2 * num3;
		static float Sigmoid(float x, float a, float b)
		{
			return 1f / (1f + Mathf.Exp((0f - a) * (x + b)));
		}
	}

	public static (float coalRate, float waterRate) CalculateCoalWaterConsumption(float throttle, float maximumTractiveEffort)
	{
		if (throttle < 0.01f)
		{
			return (coalRate: 0f, waterRate: 0f);
		}
		float num = throttle * maximumTractiveEffort;
		float num2 = 2f * num * 2f / 35000f;
		float item = num2 * 0.72f;
		return (coalRate: num2, waterRate: item);
	}

	public static float MaximumSpeedMphForCurve(float curveDegrees, float equipmentCurveLimit)
	{
		float num = MaximumSpeedMphForCurve(curveDegrees);
		if (curveDegrees < equipmentCurveLimit)
		{
			return num;
		}
		return Mathf.Lerp(5f, num, Mathf.InverseLerp(equipmentCurveLimit + 15f, equipmentCurveLimit, curveDegrees));
	}

	public static float MaximumSpeedMphForCurve(float curveDegrees)
	{
		if (curveDegrees == 0f)
		{
			return 1000f;
		}
		return 143f * Mathf.Pow(curveDegrees, -0.57f);
	}

	private static float ParametricBlend(float t)
	{
		t = Mathf.Clamp01(t);
		float num = t * t;
		return num / (2f * (num - t) + 1f);
	}

	private static float EaseIn(float t, float easing)
	{
		t = Mathf.Clamp01(t);
		float num = Mathf.Pow(2f, 1f / easing);
		return Mathf.Clamp01(2f * Mathf.Pow(t / num, easing));
	}

	private static float EaseIn4(float t)
	{
		t = Mathf.Clamp01(t);
		return Mathf.Clamp01(2f * Mathf.Pow(t / 1.1892071f, 4f));
	}

	public static float DamageForSpeed(float velocityMps, float limitMps)
	{
		if (velocityMps < limitMps)
		{
			return 0f;
		}
		return Mathf.InverseLerp(limitMps + 1.3411179f, limitMps + 2.6822357f, velocityMps) * 0.025f;
	}

	public static float DerailmentForSpeedOnCurve(float velocityMps, float limitMps)
	{
		float num = 2.6822357f;
		float num2 = 7.1526284f;
		if (velocityMps < limitMps + num)
		{
			return 0f;
		}
		return Mathf.InverseLerp(limitMps + num, limitMps + num2, velocityMps) * 0.1f;
	}

	public static float CalculateWaterConsumption(float regulator, float reverser, float wheelVelocityMph, float maximumBoilerPressure, float pistonStrokeInches, float pistonDiameterInches, float driverDiameterInches)
	{
		if (regulator < 0.01f)
		{
			return 0f;
		}
		float num = Mathf.Lerp(0.2f, 1f, Mathf.Clamp01(regulator));
		float num2 = Mathf.Lerp(pistonStrokeInches * 0.15f, pistonStrokeInches * 0.85f, Mathf.Abs(reverser));
		float num3 = 0.00211f * maximumBoilerPressure + 0.0472f;
		float num4 = Mathf.Pow(pistonDiameterInches, 2f) * num2 * num3 * 4.4f / driverDiameterInches;
		return num * Mathf.Abs(wheelVelocityMph) * num4 / 3600f;
	}

	public static float InferCoalConsumption(float gallonsPerSecond, float grateSqFt)
	{
		float num = gallonsPerSecond * 8.33f * 3600f / grateSqFt;
		return (7.503f - 0.000186f * num + 0.000312f * num * num) * grateSqFt / 3600f * Mathf.InverseLerp(0f, 0.1f, gallonsPerSecond);
	}
}
