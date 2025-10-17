using System;
using UnityEngine;

namespace Model.Physics;

public class SteamEngine : MonoBehaviour
{
	public int numberOfCylinders = 2;

	public float pistonDiameterInches = 20f;

	public float pistonStrokeInches = 26f;

	public float maximumBoilerPressure = 200f;

	public float driverDiameterInches = 63f;

	public float weightOnDrivers = 108000f;

	public float totalHeatingSurface = 2896f;

	[Range(-1f, 1f)]
	public float reverser;

	[Range(0f, 1f)]
	public float regulator;

	public bool running = true;

	public float tractiveEffort;

	public float pressure;

	[NonSerialized]
	public float MaximumSpeedMph = 75f;

	[NonSerialized]
	public float? OverrideStartingTractiveEffort;

	private float _reverserPowerMultiplier;

	private float _estimatedGrateSqFt;

	public float MaximumTractiveEffort { get; private set; } = 28000f;

	public float NormalizedTractiveEffort => Mathf.Clamp01(Mathf.Abs(tractiveEffort) / MaximumTractiveEffort);

	public float WaterConsumptionRate { get; private set; }

	public float CoalConsumptionRate { get; private set; }

	public bool HasWaterAndCoal { get; set; } = true;

	public void UpdateMaximumTractiveEffort()
	{
		MaximumTractiveEffort = CalculateTractiveEffort(maximumBoilerPressure, 0f);
		pressure = maximumBoilerPressure;
		_estimatedGrateSqFt = EstimateGrateSqFt(MaximumTractiveEffort);
		static float EstimateGrateSqFt(float te)
		{
			return 2.3f + 0.000236f * te + 1.427E-08f * te * te;
		}
	}

	public float MaximumTractiveEffortAtVelocity(float absVelocityMph)
	{
		return CalculateTractiveEffort(pressure, absVelocityMph);
	}

	public float CalculateTractiveEffort(float wheelVelocityMph)
	{
		float num = (float)((!(reverser < 0f)) ? 1 : (-1)) * regulator;
		tractiveEffort = (HasWaterAndCoal ? (num * MaximumTractiveEffortAtVelocity(Mathf.Abs(wheelVelocityMph))) : 0f);
		_reverserPowerMultiplier = TrainMath.ReverserPowerMultiplier(Mathf.Abs(reverser), Mathf.Abs(wheelVelocityMph), MaximumSpeedMph);
		tractiveEffort *= _reverserPowerMultiplier;
		UpdateConsumption(wheelVelocityMph);
		return tractiveEffort;
	}

	private void UpdateConsumption(float wheelVelocityMph)
	{
		float num = TrainMath.CalculateWaterConsumption(regulator, reverser, wheelVelocityMph, maximumBoilerPressure, pistonStrokeInches, pistonDiameterInches, driverDiameterInches);
		float num2 = TrainMath.InferCoalConsumption(num, _estimatedGrateSqFt);
		float num3 = num * 1.3f;
		WaterConsumptionRate = Mathf.Lerp(WaterConsumptionRate, num3, Time.deltaTime);
		CoalConsumptionRate = Mathf.Lerp(CoalConsumptionRate, num2, Time.deltaTime);
		if (WaterConsumptionRate < 0.001f && num3 < WaterConsumptionRate)
		{
			WaterConsumptionRate = 0f;
		}
		if (CoalConsumptionRate < 0.001f && num2 < CoalConsumptionRate)
		{
			CoalConsumptionRate = 0f;
		}
	}

	private float CalculateTractiveEffort(float boilerPressurePsi, float wheelVelocityMph)
	{
		return TrainMath.TractiveEffort(new TrainMath.SteamEngineCharacteristics(numberOfCylinders, pistonDiameterInches, pistonStrokeInches, driverDiameterInches, totalHeatingSurface, OverrideStartingTractiveEffort), wheelVelocityMph, boilerPressurePsi);
	}
}
