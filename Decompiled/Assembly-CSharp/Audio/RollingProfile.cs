using UnityEngine;

namespace Audio;

[CreateAssetMenu(fileName = "Rolling Profile", menuName = "Railroader/Audio/Rolling Profile", order = 0)]
public class RollingProfile : ScriptableObject
{
	public ParametricAudioComposition.Track[] tracks;

	public ParametricAudioComposition.Track[] squeals;

	public AnimationCurve mphToVolume;
}
