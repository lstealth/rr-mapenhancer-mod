using UnityEngine;

namespace RollingStock.Steam;

[CreateAssetMenu(fileName = "Chuff Filter Profile", menuName = "Railroader/Chuff Filter Profile", order = 0)]
public class ChuffFilterProfile : ScriptableObject
{
	public AnimationCurve amplitudeCurve;

	[Header("Filter Modulation")]
	[Range(0f, 1000f)]
	public float lowPassModulation = 100f;

	[Range(0.1f, 2f)]
	public float lowPassModulationSpeed = 0.4f;

	[Header("Speed References")]
	public float speedLow = 1f;

	public float speedHigh = 10f;

	[Header("Reverb Filter Settings: Small")]
	public float reverbLevelLowSmall = 2000f;

	public float reverbLevelHighSmall = 2000f;

	public float reflectionsLevelLowSmall = -10000f;

	public float reflectionsLevelHighSmall = -10000f;

	[Header("Reverb Filter Settings: Large")]
	public float reverbLevelLowLarge = 2000f;

	public float reverbLevelHighLarge = 2000f;

	public float reflectionsLevelLowLarge = -10000f;

	public float reflectionsLevelHighLarge = -10000f;

	[Header("Pitch Influence")]
	public AnimationCurve sizeToHighPassCutoff = AnimationCurve.Linear(0f, 200f, 1f, 100f);

	public AnimationCurve sizeToLowPassCutoff = AnimationCurve.Linear(0f, 6500f, 1f, 5500f);

	public AnimationCurve lowPassOffsetForSpeed = AnimationCurve.Linear(0f, 0f, 20f, 0f);

	public AnimationCurve highPassOffsetForSpeed = AnimationCurve.Linear(0f, 0f, 20f, 0f);

	[Header("Other")]
	public float maximumChuffDuration = 0.6f;

	public float fullCutoffMultiplier = 1.3f;

	public AnimationCurve throttleToVolumeCurve = AnimationCurve.Constant(0f, 1f, 1f);

	public AnimationCurve sizeToVolumeCurve = AnimationCurve.Constant(0f, 1f, 1f);

	[Tooltip("Position of envelope peak in time for a given normalized attack value.")]
	public AnimationCurve attackTime = AnimationCurve.Linear(0f, 0.138f, 1f, 0.2f);
}
