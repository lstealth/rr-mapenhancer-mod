using UnityEngine;

namespace Model;

[CreateAssetMenu(fileName = "Config", menuName = "Railroader/Config", order = 0)]
public class Config : ScriptableObject
{
	private static Config _shared;

	[Header("Brake Force")]
	public float brakeForceMultiplier = 1f;

	public float brakeForceMultiplierHandbrake = 2f;

	public AnimationCurve brakeForceCurve = AnimationCurve.Constant(0f, 60f, 0.4f);

	public float manualMoveCarForce = 10f;

	[Header("Sway Parameters")]
	[SerializeField]
	[Range(0.1f, 2f)]
	public float swayNoiseScale = 0.5f;

	[SerializeField]
	[Range(0f, 100f)]
	public float swayImpulseScale;

	[SerializeField]
	[Range(0f, 2f)]
	public float swayScaleRoll = 1f;

	[SerializeField]
	[Range(0f, 10f)]
	public float swaySpringStiffness = 8f;

	[SerializeField]
	[Range(0f, 2f)]
	public float swaySpringDamping = 0.5f;

	[SerializeField]
	[Range(0.01f, 1000f)]
	public float swaySpringMass = 1f;

	[SerializeField]
	[Range(0f, 10f)]
	public float swayCurveScale = 6f;

	[Tooltip("Car tons to sway mass coefficient.")]
	public AnimationCurve swayCarTonsMassCoeff = AnimationCurve.Linear(0.31f, 0.85f, 185f, 1.1f);

	[Tooltip("Time axis is miles per hour, value is component 0-1.")]
	public AnimationCurve swayComponentSpeedMph = AnimationCurve.Linear(1f, 0f, 30f, 1f);

	[Tooltip("Time axis is curve degrees, value is component 0-1.")]
	public AnimationCurve swayComponentCurveDegrees = AnimationCurve.Linear(0f, 0f, 15f, 1f);

	[Header("Wear & Tear")]
	public AnimationCurve damageForCollisionMph;

	public AnimationCurve tractiveEffortMultForCondition = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[Tooltip("Multiplied by movement delta to get service distance delta for a normalized tractive effort output. Higher TE -> higher service miles.")]
	public AnimationCurve serviceDistanceTractiveEffortMultiplier = AnimationCurve.Constant(0f, 1f, 1f);

	[Tooltip("Multiplied by movement delta to get service distance delta for a given condition.")]
	public AnimationCurve serviceDistanceConditionMultiplier = AnimationCurve.Constant(0f, 1f, 1f);

	[Tooltip("Value in percentage points. 1 = 1%.")]
	public AnimationCurve wearPerMileForCondition = AnimationCurve.Linear(0f, 0.01f, 1f, 0.01f);

	[Tooltip("Wear multiplier (for movement) based on current Oiled level. Value in percentage points. 1 = 1%.")]
	public AnimationCurve wearMultiplierForOil = AnimationCurve.Linear(0f, 2f, 1f, 1f);

	[Tooltip("Value in percentage points. 1 = 1%.")]
	public AnimationCurve oilUsePerMileForCondition = AnimationCurve.Linear(0f, 0.01f, 1f, 0.01f);

	[Tooltip("Distribution of initial 'oiled' values for cars appearing at interchange.")]
	public AnimationCurve initialOiledDistribution = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[Tooltip("Chance of hotbox for the given oil level, per 100 miles.")]
	public AnimationCurve hotboxChanceForOil = AnimationCurve.Linear(0f, 0f, 1f, 0f);

	[Tooltip("For hotbox cars, wear to be applied per mile for speed in miles per hour. 1 = 1%.")]
	public AnimationCurve hotboxWearPerMileForSpeed = AnimationCurve.Linear(0f, 0f, 1f, 0f);

	[Header("Repairs")]
	public AnimationCurve workPerPercentForConditionSteam = AnimationCurve.Linear(0f, 0.03f, 1f, 0.01f);

	public AnimationCurve workPerPercentForCondition = AnimationCurve.Linear(0f, 0.03f, 1f, 0.01f);

	public AnimationCurve repairSpeedForNormalizedCost = AnimationCurve.Linear(0f, 1f, 1f, 0.3f);

	[Header("Ladders")]
	public AnimationCurve ladderMovementSpeedCurve = AnimationCurve.Constant(0f, 1f, 1f);

	public Vector3 ladderExitBump = new Vector3(0f, 0.6f, 0.6f);

	[Header("Model Unloading")]
	public float carModelUnloadDelay = 300f;

	[Header("Ops: Population")]
	public AnimationCurve industrySpanCarLengthsToEmployees;

	public AnimationCurve passengerDepartureImmediacyToMultiplier = AnimationCurve.Linear(0f, 1f, 1f, 4f);

	public AnimationCurve passengerDepartureImmediacyToCoefficient = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	public AnimationCurve passengerDeparturePastToCoefficient = AnimationCurve.Linear(0f, 1f, 1f, 0f);

	public AnimationCurve passengerDepartureImmediacyGrowthChance = AnimationCurve.Linear(0f, 0.3f, 1f, 0f);

	[Header("Doppler")]
	public AnimationCurve cameraVelocityToDoppler = AnimationCurve.Linear(0f, 1f, 1f, 0f);

	public float dopplerDeltaIncreasing = 1f;

	public float dopplerDeltaDecreasing = 5f;

	[Header("Character")]
	public AnimationCurve characterEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

	public float characterEasingSpeed = 1f;

	public static Config Shared
	{
		get
		{
			if (!_shared)
			{
				return _shared = TrainController.Shared.config;
			}
			return _shared;
		}
	}
}
