using Model.Physics;
using UnityEngine;

namespace RollingStock;

public abstract class LocomotiveControlAdapter : MonoBehaviour
{
	public LocomotiveAirSystem air;

	public LocomotiveAudio audio;

	public abstract int ThrottleInputNotches { get; }

	public abstract int ThrottleValueSteps { get; }

	public abstract float AbstractReverser { get; set; }

	public abstract float AbstractThrottle { get; set; }

	public virtual int ThrottleDisplay => Mathf.RoundToInt(AbstractThrottle * (float)ThrottleValueSteps);

	public float LocomotiveBrakeSetting
	{
		get
		{
			return air.locomotiveBrakeSetting;
		}
		set
		{
			air.locomotiveBrakeSetting = value;
		}
	}

	public float TrainBrakeSetting
	{
		get
		{
			return air.trainBrakeSetting;
		}
		set
		{
			air.trainBrakeSetting = value;
		}
	}

	public float LocomotiveBrakePressure
	{
		get
		{
			return air.locomotiveBrakePressure;
		}
		set
		{
			air.locomotiveBrakePressure = value;
		}
	}

	public float Horn
	{
		get
		{
			if (audio.whistle != null)
			{
				return audio.whistle.parameter;
			}
			if (audio.horn != null)
			{
				return audio.horn.value;
			}
			return 0f;
		}
		set
		{
			if (audio.whistle != null)
			{
				audio.whistle.parameter = value;
			}
			else if (audio.horn != null)
			{
				audio.horn.value = value;
			}
		}
	}

	public bool Bell
	{
		get
		{
			return audio.bell.IsOn;
		}
		set
		{
			if (audio.bell != null)
			{
				audio.bell.IsOn = value;
			}
		}
	}

	public abstract float NormalizedTractiveEffort { get; }
}
