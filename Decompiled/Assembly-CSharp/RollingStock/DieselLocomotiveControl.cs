using Model.Physics;
using UnityEngine;

namespace RollingStock;

public class DieselLocomotiveControl : LocomotiveControlAdapter
{
	public PrimeMover primeMover;

	public override int ThrottleInputNotches => 8;

	public override int ThrottleValueSteps => 8;

	public override float AbstractReverser
	{
		get
		{
			return primeMover.reverser;
		}
		set
		{
			primeMover.reverser = Mathf.RoundToInt(value);
		}
	}

	public override float AbstractThrottle
	{
		get
		{
			return (float)primeMover.notch / 8f;
		}
		set
		{
			primeMover.notch = Mathf.RoundToInt(value * 8f);
			if (audio != null && audio.primeMover != null)
			{
				audio.primeMover.Notch = primeMover.notch;
			}
		}
	}

	public override float NormalizedTractiveEffort => primeMover.NormalizedTractiveEffort;
}
