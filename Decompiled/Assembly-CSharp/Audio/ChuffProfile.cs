using UnityEngine;

namespace Audio;

[CreateAssetMenu(fileName = "Chuff", menuName = "Train Game/Chuff Profile", order = 0)]
public class ChuffProfile : ScriptableObject
{
	[Header("Particles")]
	public AnimationCurve effortToAlpha = AnimationCurve.Constant(0f, 1f, 1f);

	public AnimationCurve effortToVelocity = AnimationCurve.Linear(0f, 3f, 1f, 20f);

	public AnimationCurve effortToSize = AnimationCurve.Linear(0f, 0.4f, 1f, 0.8f);

	public AnimationCurve effortToLifetime = AnimationCurve.Linear(0f, 4f, 1f, 8f);

	public AnimationCurve effortToRate = AnimationCurve.Linear(0f, 200f, 1f, 400f);

	[Header("Audio")]
	public AnimationCurve rolloffCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
}
