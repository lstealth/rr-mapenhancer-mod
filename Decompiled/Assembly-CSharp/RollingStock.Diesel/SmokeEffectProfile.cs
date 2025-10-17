using UnityEngine;

namespace RollingStock.Diesel;

[CreateAssetMenu(fileName = "Smoke Effect Profile", menuName = "Railroader/Smoke Effect Profile", order = 0)]
public class SmokeEffectProfile : ScriptableObject
{
	public Gradient colorGradient;

	public AnimationCurve velocityCurve = AnimationCurve.Linear(0f, 3f, 1f, 20f);

	public AnimationCurve sizeCurve = AnimationCurve.Linear(0f, 0.4f, 1f, 0.8f);

	public AnimationCurve lifetimeCurve = AnimationCurve.Linear(0f, 4f, 1f, 8f);

	public AnimationCurve rateCurve = AnimationCurve.Linear(0f, 200f, 1f, 400f);

	public AnimationCurve turbulenceCurve = AnimationCurve.Linear(0f, 10f, 1f, 10f);
}
