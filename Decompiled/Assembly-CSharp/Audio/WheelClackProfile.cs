using UnityEngine;

namespace Audio;

[CreateAssetMenu(fileName = "Wheel Clack Profile", menuName = "Railroader/Audio/Wheel Clack Profile", order = 0)]
public class WheelClackProfile : ScriptableObject
{
	public AudioClip wheelClackClip;

	public float jointDistance = 24f;

	[Header("Volume")]
	public AnimationCurve velocityMphToVolume = AnimationCurve.Linear(5f, 0.1f, 20f, 1f);

	[Header("Volume Envelope")]
	public AnimationCurve velocityMphToDuration = AnimationCurve.Constant(0f, 40f, 1f);

	public AnimationCurve volumeEnvelope = AnimationCurve.Linear(0f, 1f, 1f, 1f);

	[Header("High & Low Pass Cutoffs")]
	public AnimationCurve velocityMphToHighPassCutoff = AnimationCurve.Constant(0f, 40f, 0f);

	public AnimationCurve velocityMphToLowPassCutoff = AnimationCurve.Constant(0f, 40f, 10000f);

	[Header("Low Pass Noise")]
	public float lowPassNoiseMagnitude;

	[Header("Rolloff")]
	public float rolloffMinDistance = 2f;

	public float rolloffMaxDistance = 50f;
}
