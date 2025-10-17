using System;
using Game.Messages;
using Model;
using Model.Definition;
using RollingStock;
using RollingStock.Controls;
using UI;
using UI.Common;
using UnityEngine;

[RequireComponent(typeof(TrainController))]
public class TrainInput : MonoBehaviour
{
	private TrainController _trainController;

	private float _hornWas;

	private float _hornDownMousePosition;

	private float _locomotiveBrakeDelta;

	private float _trainBrakeDelta;

	private float _throttleDelta;

	private BaseLocomotive SelectedLocomotive => _trainController.SelectedLocomotive;

	private void Start()
	{
		_trainController = GetComponent<TrainController>();
	}

	private void Update()
	{
		if (!TryGetLocomotiveControlAdapter(out var loco, out var adapter))
		{
			return;
		}
		GameInput shared = GameInput.shared;
		bool isShiftDown = GameInput.IsShiftDown;
		bool isControlDown = GameInput.IsControlDown;
		float num = (isShiftDown ? 0.05f : 0.01f);
		float num2 = 0f;
		float throttleDelta = 0f;
		float trainBrakeDelta = 0f;
		float num3 = 0f;
		if (loco.Archetype == CarArchetype.LocomotiveDiesel)
		{
			if (shared.ReverserBack)
			{
				num2 = -1f;
			}
			else if (shared.ReverserForward)
			{
				num2 = 1f;
			}
		}
		else
		{
			float num4 = (isControlDown ? 1f : 0.1f);
			if (shared.ReverserBackRepeating)
			{
				num2 = 0f - num4;
			}
			else if (shared.ReverserForwardRepeating)
			{
				num2 = num4;
			}
		}
		int throttleInputNotches = adapter.ThrottleInputNotches;
		if (throttleInputNotches > 0)
		{
			int num5 = 0;
			if (shared.ThrottleDown)
			{
				num5 = -1;
			}
			else if (shared.ThrottleUp)
			{
				num5 = 1;
			}
			if (num5 > 0 && adapter.AbstractThrottle < 1f)
			{
				ChangeValue(PropertyChange.Control.Throttle, Mathf.Clamp01(adapter.AbstractThrottle + 1f / (float)throttleInputNotches));
			}
			if (num5 < 0 && adapter.AbstractThrottle > 0f)
			{
				ChangeValue(PropertyChange.Control.Throttle, Mathf.Clamp01(adapter.AbstractThrottle - 1f / (float)throttleInputNotches));
			}
		}
		else
		{
			float num6 = (isShiftDown ? 0.15f : 0.03f);
			if (shared.ThrottleDownRepeating)
			{
				throttleDelta = 0f - num6;
			}
			else if (shared.ThrottleUpRepeating)
			{
				throttleDelta = num6;
			}
		}
		if (shared.TrainBrakeRelease)
		{
			trainBrakeDelta = (0f - num) * 2f;
		}
		else if (shared.TrainBrakeApply)
		{
			trainBrakeDelta = num;
		}
		if (shared.LocomotiveBrakeRelease)
		{
			num3 = (0f - num) * 2f;
		}
		else if (shared.LocomotiveBrakeApply)
		{
			num3 = num;
		}
		if (num2 != 0f)
		{
			ChangeValue(PropertyChange.Control.Reverser, Mathf.Clamp(adapter.AbstractReverser + num2, -1f, 1f));
		}
		if (_locomotiveBrakeDelta < 0f && adapter.LocomotiveBrakeSetting < 0f && num3 == 0f)
		{
			ChangeValue(PropertyChange.Control.LocomotiveBrake, 0f);
		}
		_locomotiveBrakeDelta = num3;
		_trainBrakeDelta = trainBrakeDelta;
		_throttleDelta = throttleDelta;
		if (shared.Bell)
		{
			loco.ControlProperties[PropertyChange.Control.Bell] = !loco.ControlProperties[PropertyChange.Control.Bell];
		}
		if (shared.CylinderCock)
		{
			loco.ControlProperties[PropertyChange.Control.CylinderCock] = !loco.ControlProperties[PropertyChange.Control.CylinderCock];
		}
		float num7 = shared.InputHorn;
		if (shared.HornExpressionEnabledThisFrame)
		{
			_hornDownMousePosition = shared.HornExpressionValue;
		}
		if (shared.HornExpressionEnabled)
		{
			float hornExpressionValue = shared.HornExpressionValue;
			_hornDownMousePosition += hornExpressionValue;
			num7 = Mathf.Clamp01((0f - _hornDownMousePosition) / 200f);
		}
		if (Math.Abs(num7 - _hornWas) > 0.01f)
		{
			loco.ControlProperties[PropertyChange.Control.Horn] = num7;
			_hornWas = num7;
		}
		int inputHeadlight = shared.InputHeadlight;
		if (inputHeadlight != 0)
		{
			HeadlightToggleLogic.State state = HeadlightToggleLogic.SetHeadlightStateOffset(loco.KeyValueObject, inputHeadlight);
			Toast.Present("Headlight: " + HeadlightToggleLogic.TextForState(state), ToastPosition.Bottom);
		}
	}

	private void FixedUpdate()
	{
		if (!TryGetLocomotiveControlAdapter(out var _, out var adapter))
		{
			return;
		}
		if (_locomotiveBrakeDelta != 0f)
		{
			float locomotiveBrakeSetting = adapter.LocomotiveBrakeSetting;
			float value = Mathf.Clamp(locomotiveBrakeSetting + _locomotiveBrakeDelta, -0.1f, 1f);
			if (locomotiveBrakeSetting <= 0f && _locomotiveBrakeDelta < 0f)
			{
				value = -0.1f;
			}
			ChangeValue(PropertyChange.Control.LocomotiveBrake, value);
		}
		if (_trainBrakeDelta != 0f)
		{
			float value2 = Mathf.Clamp(adapter.TrainBrakeSetting + _trainBrakeDelta, 0f, 1f);
			ChangeValue(PropertyChange.Control.TrainBrake, value2);
		}
		if (_throttleDelta != 0f)
		{
			float value3 = Mathf.Clamp01(adapter.AbstractThrottle + _throttleDelta);
			ChangeValue(PropertyChange.Control.Throttle, value3);
		}
	}

	private bool TryGetLocomotiveControlAdapter(out BaseLocomotive loco, out LocomotiveControlAdapter adapter)
	{
		adapter = null;
		loco = null;
		Car selectedCar = _trainController.SelectedCar;
		if (selectedCar == null || !selectedCar.IsLocomotive)
		{
			return false;
		}
		if (!GameInput.MovementInputEnabled)
		{
			return false;
		}
		loco = (BaseLocomotive)selectedCar;
		adapter = loco.locomotiveControl;
		return true;
	}

	private void ChangeValue(PropertyChange.Control control, float value)
	{
		BaseLocomotive selectedLocomotive = SelectedLocomotive;
		if (!(selectedLocomotive == null))
		{
			selectedLocomotive.SendPropertyChange(control, value);
		}
	}
}
