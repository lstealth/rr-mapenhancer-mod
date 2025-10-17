using UnityEngine;

namespace Audio;

[CreateAssetMenu(fileName = "Prime Mover Audio Profile", menuName = "Train Game/Audio/Prime Mover Audio Profile", order = 0)]
public class PrimeMoverAudioProfile : ScriptableObject
{
	public AudioClip[] notchLoops = new AudioClip[9];

	public AudioClip[] transitionsUp = new AudioClip[8];

	public AudioClip[] transitionsDown = new AudioClip[8];

	public AnimationCurve rolloffCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

	[Range(0f, 1f)]
	public float volume = 1f;
}
