using UnityEngine;

namespace Audio;

[CreateAssetMenu(fileName = "Whistle", menuName = "Train Game/Whistle Profile", order = 10)]
public class WhistleProfile : ScriptableObject
{
	public AudioClip audioClip;

	[Range(0f, 1f)]
	public float rampUpPitch = 0.9f;

	[Tooltip("How quickly does the whistle change in response to parameter changes?")]
	public float lerpSpeed = 10f;

	public float airLerpSpeed = 40f;

	public AnimationCurve parameterToPitch = AnimationCurve.Constant(0f, 1f, 1f);

	public AnimationCurve parameterToVolume = AnimationCurve.Constant(0f, 1f, 1f);
}
