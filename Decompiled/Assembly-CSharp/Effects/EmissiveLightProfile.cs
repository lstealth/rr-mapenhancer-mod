using UnityEngine;

namespace Effects;

[CreateAssetMenu(fileName = "Emissive Light Profile", menuName = "Train Game/Effects/Emissive Light Profile", order = 0)]
public class EmissiveLightProfile : ScriptableObject
{
	[Header("Light Settings")]
	[Range(0f, 10000f)]
	public float lightIntensityDay = 1000f;

	[Range(0f, 10000f)]
	public float lightIntensityNight = 6000f;

	[Range(0f, 2f)]
	public float lightIntensityDimMultiplier = 0.5f;

	[Header("Bloom Settings")]
	[Range(1f, 1000f)]
	public float intensity = 57f;

	[Range(0f, 1f)]
	public float dimValueMaximum = 1f;

	[Range(0f, 1f)]
	public float minimumMultiplier = 0.1f;

	[Range(0f, 90f)]
	public float fullBeamAngle = 2f;

	[Range(0f, 90f)]
	public float minimumBeamAngle = 8f;

	public Color emissionColor = Color.white;

	public AnimationCurve scaleCurve = AnimationCurve.Constant(0f, 1000f, 1f);

	public AnimationCurve intensityCurve = AnimationCurve.Constant(0f, 1000f, 1f);

	public AnimationCurve angleScaleCurve = AnimationCurve.Constant(0f, 1000f, 1f);
}
