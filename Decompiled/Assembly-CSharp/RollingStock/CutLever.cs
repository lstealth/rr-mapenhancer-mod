using System;
using RollingStock.ContinuousControls;
using UnityEngine;

namespace RollingStock;

public class CutLever : MonoBehaviour
{
	public ContinuousControl control;

	private bool _primed = true;

	public event Action OnActivate;

	private void OnEnable()
	{
		control.OnValueChanged -= ControlDidChange;
		control.OnValueChanged += ControlDidChange;
	}

	private void OnDisable()
	{
		control.OnValueChanged -= ControlDidChange;
	}

	private void ControlDidChange(float value)
	{
		if (_primed && value > 0.5f)
		{
			Debug.Log("Cut!");
			this.OnActivate?.Invoke();
			_primed = false;
		}
		else if (!_primed && value < 0.1f)
		{
			_primed = true;
		}
	}
}
