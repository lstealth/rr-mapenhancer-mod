using Game.Messages;
using KeyValue.Runtime;
using Model.Definition;
using UnityEngine;

namespace Model;

public class LocomotiveControlHelper
{
	private readonly BaseLocomotive _locomotive;

	private const float FeedValve = 90f;

	public float Throttle
	{
		get
		{
			return GetValue(PropertyChange.Control.Throttle).FloatValue;
		}
		set
		{
			ChangeValue(PropertyChange.Control.Throttle, Mathf.Clamp01(value));
		}
	}

	public float Reverser
	{
		get
		{
			return GetValue(PropertyChange.Control.Reverser).FloatValue;
		}
		set
		{
			ChangeValue(PropertyChange.Control.Reverser, Mathf.Clamp(value, -1f, 1f));
		}
	}

	public float LocomotiveBrake
	{
		get
		{
			return GetValue(PropertyChange.Control.LocomotiveBrake).FloatValue;
		}
		set
		{
			ChangeValue(PropertyChange.Control.LocomotiveBrake, Mathf.Clamp01(value));
		}
	}

	public float TrainBrake
	{
		get
		{
			return GetValue(PropertyChange.Control.TrainBrake).FloatValue;
		}
		set
		{
			ChangeValue(PropertyChange.Control.TrainBrake, Mathf.Clamp01(value));
		}
	}

	public bool Bell
	{
		get
		{
			return GetValue(PropertyChange.Control.Bell).BoolValue;
		}
		set
		{
			ChangeValue(PropertyChange.Control.Bell, value);
		}
	}

	public float Horn
	{
		get
		{
			return GetValue(PropertyChange.Control.Horn).FloatValue;
		}
		set
		{
			ChangeValue(PropertyChange.Control.Horn, value);
		}
	}

	public bool CylinderCocksOpen
	{
		get
		{
			return GetValue(PropertyChange.Control.CylinderCock).BoolValue;
		}
		set
		{
			if (_locomotive.Archetype == CarArchetype.LocomotiveSteam)
			{
				ChangeValue(PropertyChange.Control.CylinderCock, value);
			}
		}
	}

	public float TrainBrakeSet => Mathf.Lerp(0f, 90f, TrainBrake);

	public LocomotiveControlHelper(BaseLocomotive locomotive)
	{
		_locomotive = locomotive;
	}

	public void TrainBrakeMakeSet(float psi)
	{
		float trainBrake = TrainBrake;
		TrainBrake = trainBrake + Mathf.InverseLerp(0f, 90f, psi);
	}

	private void ChangeValue(PropertyChange.Control control, bool value)
	{
		_locomotive.SendPropertyChange(control, value);
	}

	private void ChangeValue(PropertyChange.Control control, float value)
	{
		_locomotive.SendPropertyChange(control, value);
	}

	private Value GetValue(PropertyChange.Control control)
	{
		return _locomotive.KeyValueObject[PropertyChange.KeyForControl(control)];
	}

	public void BailOff()
	{
		ChangeValue(PropertyChange.Control.LocomotiveBrake, -0.1f);
	}
}
