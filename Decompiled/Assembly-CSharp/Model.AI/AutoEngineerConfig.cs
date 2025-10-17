using UnityEngine;

namespace Model.AI;

[CreateAssetMenu(fileName = "AutoEngineerConfig", menuName = "Railroader/AutoEngineer Config", order = 0)]
public class AutoEngineerConfig : ScriptableObject
{
	public float fullBrakeSet = 26f;

	public float weightTonsLight = 500f;

	public float weightTonsHeavy = 1000f;

	[Tooltip("Time is distance in meters, value is MPH")]
	public AnimationCurve maxVelocityForDistanceLight = AnimationCurve.Linear(0f, 0f, 200f, 100f);

	[Tooltip("Time is distance in meters, value is MPH")]
	public AnimationCurve maxVelocityForDistanceHeavy = AnimationCurve.Linear(0f, 0f, 200f, 100f);

	[Header("PID Settings")]
	public PIDController throttlePID;

	public PIDController independentPID;

	public PIDController trainBrakePID;

	[Range(0f, 4f)]
	public float brakeErrorPower = 1f;

	[Tooltip("MPH to inflate current speed, given the speed in MPH. To allow headroom.")]
	public AnimationCurve paddingForSpeedMph = AnimationCurve.Linear(5f, 0f, 20f, 1f);

	public AnimationCurve trainBrakeDerivativeGainForNumAirOpenCars = AnimationCurve.Linear(10f, -0.5f, 30f, -1.2f);

	[Range(-0.2f, 0.1f)]
	public float trainBrakeReleaseBelowOutput = -0.01f;

	public AnimationCurve[] crossingWhistlePatterns;

	public float minimumTimeBetweenCrossingWhistles = 5f;

	[Header("Train Brake State Machine")]
	public AnimationCurve applyTimeForNumberOfCars;

	public AnimationCurve releaseTimeForNumberOfCars;

	public AnimationCurve brakeSetForDeltaVelocityMph;

	public AnimationCurve brakeSetMultiplierForVelocityMph;

	[Header("AEWP")]
	public float momentumFactor = 2f;

	public float momentumOffset = 50f;

	public float momentumRerouteAtCtcSwitch = 2000f;
}
