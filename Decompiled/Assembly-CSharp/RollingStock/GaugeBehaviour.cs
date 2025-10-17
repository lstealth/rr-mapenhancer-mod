using UnityEngine;

namespace RollingStock;

public class GaugeBehaviour : MonoBehaviour, IGauge
{
	private float _value;

	public float Value
	{
		get
		{
			return _value;
		}
		set
		{
			if (!(Mathf.Abs(value - _value) < 1E-06f))
			{
				_value = value;
				ValueDidChange();
			}
		}
	}

	protected virtual void ValueDidChange()
	{
	}
}
