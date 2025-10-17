using UnityEngine;

namespace Model.Physics;

public class PrimeMover : MonoBehaviour
{
	[Tooltip("Maximum tractive effort for this diesel prime mover.")]
	public int startingTractiveEffort = 49500;

	public bool running = true;

	public int reverser;

	public int notch;

	public float tractiveEffort;

	public float amps;

	public float rpms;

	private const int MaxAmps = 900;

	private readonly int[] _notchToRpm = new int[9] { 300, 362, 425, 487, 550, 613, 675, 738, 800 };

	private readonly float[] _notchToPowerPercent = new float[9] { 0f, 0.04f, 0.13f, 0.23f, 0.35f, 0.5f, 0.65f, 0.83f, 1f };

	private float actualPowerPercent;

	public float NormalizedTractiveEffort => Mathf.Clamp01(Mathf.Abs(tractiveEffort) / (float)startingTractiveEffort);

	public float FuelConsumptionRate
	{
		get
		{
			if (notch < 1)
			{
				return 0f;
			}
			float num = (float)(notch * startingTractiveEffort) / 64750f;
			float num2 = -0.5957576f + 4.960411f * num + 1.051407f * num * num;
			return Mathf.Max(0f, num2 / 3600f);
		}
	}

	public bool HasFuel { get; set; } = true;

	public float CalculateTractiveEffort(float absMph)
	{
		if (!running || !HasFuel)
		{
			amps = 0f;
			rpms = 0f;
			tractiveEffort = 0f;
			return 0f;
		}
		float num = _notchToPowerPercent[notch];
		float maxDelta = Time.deltaTime * ((actualPowerPercent < num) ? 0.1f : 0.5f);
		actualPowerPercent = Mathf.MoveTowards(actualPowerPercent, num, maxDelta);
		float num2 = actualPowerPercent * (float)reverser;
		tractiveEffort = num2 * MaxTractiveEffort(absMph);
		amps = CalculateAmps(absMph);
		rpms = _notchToRpm[notch];
		return tractiveEffort;
	}

	public float MaxTractiveEffort(float absMph)
	{
		return CalculateTractiveEffort(absMph, startingTractiveEffort);
	}

	private static float CalculateTractiveEffort(float mph, float startingTractiveEffort)
	{
		float t = Mathf.Clamp01(Mathf.InverseLerp(0f, 10f, mph));
		float num = startingTractiveEffort / 80000f;
		if (mph < 10f)
		{
			return Mathf.Lerp(startingTractiveEffort, CalculateContinuousTractiveEffort80000(10f) * num, t);
		}
		return CalculateContinuousTractiveEffort80000(mph) * num;
	}

	private static float CalculateContinuousTractiveEffort80000(float absMph)
	{
		return 16253.46f + 201411f / Mathf.Pow(2f, absMph / 4.534249f);
	}

	private float CalculateAmps(float absMph)
	{
		int num = startingTractiveEffort;
		return actualPowerPercent * CalculateTractiveEffort(absMph, startingTractiveEffort) / (float)num * 900f;
	}
}
