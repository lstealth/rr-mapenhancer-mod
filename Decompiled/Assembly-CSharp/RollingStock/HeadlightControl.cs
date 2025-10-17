using System;
using Effects;
using Game.Messages;
using Game.State;
using KeyValue.Runtime;
using Model;
using RollingStock.ContinuousControls;
using RollingStock.Controls;
using Serilog;
using UnityEngine;

namespace RollingStock;

public class HeadlightControl : MonoBehaviour
{
	public RadialAnimatedControl control;

	public HeadlightControlStyle style;

	private IDisposable _observer;

	private string _carId;

	private KeyValueObject _keyValueObject;

	private string _headlightKey;

	private bool _controlDidChange;

	private string CarId => _carId ?? (_carId = GetComponentInParent<Car>()?.id);

	private int SnapPoints => style switch
	{
		HeadlightControlStyle.Bidirectional => 5, 
		HeadlightControlStyle.Unidirectional => 3, 
		_ => throw new ArgumentOutOfRangeException(), 
	};

	private void OnEnable()
	{
		_headlightKey = PropertyChange.KeyForControl(PropertyChange.Control.Headlight);
		control.OnValueChanged += ControlDidChange;
		control.ConfigureSnap(SnapPoints - 1);
		control.CheckAuthorized = () => StateManager.CheckAuthorizedToSendMessage(new PropertyChange(CarId, _headlightKey, new FloatPropertyValue(0f)));
		control.tooltipText = GetTooltipText;
		_observer = (_keyValueObject = GetComponentInParent<KeyValueObject>()).Observe(_headlightKey, delegate(Value value)
		{
			HandleHeadlightValueChanged(value.IntValue);
		});
	}

	private void OnDisable()
	{
		control.OnValueChanged -= ControlDidChange;
		_observer?.Dispose();
		_observer = null;
	}

	private string GetTooltipText()
	{
		switch (style)
		{
		case HeadlightControlStyle.Bidirectional:
			return HeadlightToggleLogic.TextForState(HeadlightToggleLogic.GetHeadlightState(_keyValueObject));
		case HeadlightControlStyle.Unidirectional:
		{
			HeadlightController.State item = HeadlightStateLogic.StatesFromInt(_keyValueObject[_headlightKey].IntValue).Item1;
			return StringForUnidirectional(item);
		}
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	private static string StringForUnidirectional(HeadlightController.State state)
	{
		return state switch
		{
			HeadlightController.State.Off => "Off", 
			HeadlightController.State.Dim => "Dim", 
			HeadlightController.State.On => "Full", 
			_ => throw new ArgumentOutOfRangeException("state", state, null), 
		};
	}

	private void HandleHeadlightValueChanged(int value)
	{
		if (_controlDidChange)
		{
			return;
		}
		var (state, state2) = HeadlightStateLogic.StatesFromInt(value);
		float num;
		switch (style)
		{
		case HeadlightControlStyle.Bidirectional:
			switch (state)
			{
			case HeadlightController.State.On:
				if (state2 != HeadlightController.State.Off)
				{
					goto IL_0089;
				}
				num = 0f;
				break;
			case HeadlightController.State.Dim:
				if (state2 != HeadlightController.State.Off)
				{
					goto IL_0089;
				}
				num = 0.25f;
				break;
			case HeadlightController.State.Off:
				switch (state2)
				{
				case HeadlightController.State.Off:
					break;
				case HeadlightController.State.Dim:
					goto IL_0079;
				case HeadlightController.State.On:
					goto IL_0081;
				default:
					goto IL_0089;
				}
				num = 0.5f;
				break;
			default:
				goto IL_0089;
				IL_0081:
				num = 1f;
				break;
				IL_0079:
				num = 0.75f;
				break;
				IL_0089:
				Log.Warning("Unexpected headlight value: {forward}, {rear}", state, state2);
				num = 0.5f;
				break;
			}
			break;
		case HeadlightControlStyle.Unidirectional:
			num = state switch
			{
				HeadlightController.State.Off => 0.5f, 
				HeadlightController.State.Dim => 0.75f, 
				HeadlightController.State.On => 0.25f, 
				_ => throw new ArgumentOutOfRangeException(), 
			};
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		Log.Debug("HeadlightControl: HandleHeadlightValueChanged {forward} {rear} -> {controlValue}", state, state2, num);
		control.Value = num;
	}

	private void ControlDidChange(float value)
	{
		int num = Mathf.RoundToInt(value * (float)(SnapPoints - 1));
		HeadlightController.State state = HeadlightController.State.Off;
		HeadlightController.State state2 = HeadlightController.State.Off;
		switch (style)
		{
		case HeadlightControlStyle.Bidirectional:
			switch (num)
			{
			case 0:
				state = HeadlightController.State.On;
				state2 = HeadlightController.State.Off;
				break;
			case 1:
				state = HeadlightController.State.Dim;
				state2 = HeadlightController.State.Off;
				break;
			case 2:
				state = HeadlightController.State.Off;
				state2 = HeadlightController.State.Off;
				break;
			case 3:
				state = HeadlightController.State.Off;
				state2 = HeadlightController.State.Dim;
				break;
			case 4:
				state = HeadlightController.State.Off;
				state2 = HeadlightController.State.On;
				break;
			}
			break;
		case HeadlightControlStyle.Unidirectional:
			switch (num)
			{
			case 0:
				state = HeadlightController.State.On;
				break;
			case 1:
				state = HeadlightController.State.Off;
				break;
			case 2:
				state = HeadlightController.State.Dim;
				break;
			}
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		Log.Debug("HeadlightControl: ControlDidChange {value} -> {intValue} -> {forward} {rear}", value, num, state, state2);
		int value2 = HeadlightStateLogic.IntFromStates(state, state2);
		_controlDidChange = true;
		_keyValueObject[_headlightKey] = Value.Int(value2);
		_controlDidChange = false;
	}
}
