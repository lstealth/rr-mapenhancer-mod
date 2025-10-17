using System;
using System.Collections.Generic;
using Audio;
using Game.Messages;
using Game.State;
using KeyValue.Runtime;
using RollingStock.ContinuousControls;
using Serilog;
using TMPro;
using UnityEngine;

namespace Track.Signals;

[SelectionBase]
[RequireComponent(typeof(RadialAnimatedControl))]
public class CTCPanelKnob : MonoBehaviour
{
	public enum Purpose
	{
		Switch,
		Signal,
		SystemMode
	}

	[Tooltip("Defines available positions for the knob.")]
	public Purpose purpose;

	public string knobId;

	[Tooltip("Optional. If set, displayed in the number label (instead of the id).")]
	public string displayNumber;

	[Header("Subcomponents")]
	[SerializeField]
	private TMP_Text mainLabel;

	[SerializeField]
	private TMP_Text leftLabel;

	[SerializeField]
	private TMP_Text rightLabel;

	[SerializeField]
	private TMP_Text numberLabel;

	[Header("Audio")]
	[SerializeField]
	private List<AudioClip> audioClips;

	private RadialAnimatedControl _control;

	private readonly HashSet<IDisposable> _observers = new HashSet<IDisposable>();

	private KeyValueObject _keyValueObject;

	private bool _isInitial = true;

	private string Key => CTCKeys.Knob(knobId);

	private static SystemMode SystemMode
	{
		get
		{
			if (!(CTCPanelController.Shared == null))
			{
				return CTCPanelController.Shared.SystemMode;
			}
			return SystemMode.ABS;
		}
	}

	public SwitchSetting CurrentSwitchSetting => SwitchSettingFromValue(_keyValueObject[CTCKeys.Knob(knobId)]);

	public SignalDirection CurrentDirection => DirectionFromValue(_keyValueObject[CTCKeys.Knob(knobId)]);

	private void Awake()
	{
		_control = GetComponent<RadialAnimatedControl>();
		_control.OnValueChanged += ControlOnValueChanged;
		RadialAnimatedControl control = _control;
		control.OnCustomSnap = (Func<float, float>)Delegate.Combine(control.OnCustomSnap, new Func<float, float>(ControlCustomSnap));
		_control.tooltipText = ControlTooltipText;
	}

	private void OnEnable()
	{
		if (string.IsNullOrEmpty(knobId))
		{
			Log.Error("CTCPanelKob {name} has empty id", base.name);
		}
		_keyValueObject = GetComponentInParent<KeyValueObject>();
		_isInitial = true;
		_observers.Add(_keyValueObject.Observe(Key, delegate(Value value)
		{
			OnPropertyChange(value, _isInitial);
			_isInitial = false;
		}));
		UpdateLabels();
	}

	private void OnDisable()
	{
		foreach (IDisposable observer in _observers)
		{
			observer.Dispose();
		}
		_observers.Clear();
	}

	private void OnValidate()
	{
		UpdateLabels();
	}

	public void UpdateLabels()
	{
		string text = purpose switch
		{
			Purpose.Switch => "Switch", 
			Purpose.Signal => "Signal", 
			Purpose.SystemMode => "Mode", 
			_ => "Mystery", 
		};
		string text2 = (string.IsNullOrEmpty(displayNumber) ? knobId : displayNumber);
		if (mainLabel != null)
		{
			mainLabel.text = text;
		}
		if (numberLabel != null)
		{
			numberLabel.text = text2;
		}
		var (text3, text4) = purpose switch
		{
			Purpose.Switch => ("N", "R"), 
			Purpose.Signal => ("L", "R"), 
			Purpose.SystemMode => ("ABS", "CTC"), 
			_ => ("N", "R"), 
		};
		if (leftLabel != null)
		{
			leftLabel.text = text3;
		}
		if (rightLabel != null)
		{
			rightLabel.text = text4;
		}
		if (_control != null)
		{
			_control.displayName = text + " " + text2;
		}
	}

	private float ControlCustomSnap(float value)
	{
		switch (purpose switch
		{
			Purpose.Switch => 2, 
			Purpose.Signal => 3, 
			Purpose.SystemMode => 2, 
			_ => throw new ArgumentOutOfRangeException(), 
		})
		{
		case 2:
			return (!((double)value < 0.5)) ? 1 : 0;
		case 3:
			if ((double)value < 0.25)
			{
				return 0f;
			}
			if (value < 0.75f)
			{
				return 0.5f;
			}
			return 1f;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	private string ControlTooltipText()
	{
		if (!StateManager.CheckAuthorizedToSendMessage(MessageForValueChange(0f)))
		{
			return "<sprite name=\"MouseNo\"> N/A";
		}
		switch (purpose)
		{
		case Purpose.SystemMode:
			return SystemMode switch
			{
				SystemMode.ABS => "ABS", 
				SystemMode.CTC => "CTC", 
				_ => throw new ArgumentOutOfRangeException(), 
			};
		case Purpose.Signal:
			if (SystemMode == SystemMode.ABS)
			{
				return "Inoperative in ABS";
			}
			return null;
		case Purpose.Switch:
			return null;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	private IGameMessage MessageForValueChange(float controlValue)
	{
		return new PropertyChange(value: new IntPropertyValue(purpose switch
		{
			Purpose.Switch => (!(controlValue < 0.5f)) ? 1 : 0, 
			Purpose.Signal => Mathf.RoundToInt(controlValue * 2f) switch
			{
				0 => 2, 
				1 => 0, 
				2 => 1, 
				_ => throw new ArgumentOutOfRangeException(), 
			}, 
			Purpose.SystemMode => (!(controlValue < 0.5f)) ? 1 : 0, 
			_ => throw new ArgumentOutOfRangeException(), 
		}), objectId: _keyValueObject.RegisteredId, key: Key);
	}

	private void ControlOnValueChanged(float controlValue)
	{
		StateManager.ApplyLocal(MessageForValueChange(controlValue));
	}

	private void OnPropertyChange(Value value, bool isInitial)
	{
		float value2;
		switch (purpose)
		{
		case Purpose.Switch:
			value2 = SwitchSettingFromValue(value) switch
			{
				SwitchSetting.Normal => 0, 
				SwitchSetting.Reversed => 1, 
				_ => throw new ArgumentOutOfRangeException("value", $"{value} for switch"), 
			};
			break;
		case Purpose.Signal:
			value2 = DirectionFromValue(value) switch
			{
				SignalDirection.Left => 0f, 
				SignalDirection.None => 0.5f, 
				SignalDirection.Right => 1f, 
				_ => throw new ArgumentOutOfRangeException("value", $"{value} for signal"), 
			};
			break;
		case Purpose.SystemMode:
			value2 = (SystemMode)value.IntValue switch
			{
				SystemMode.ABS => 0, 
				SystemMode.CTC => 1, 
				_ => throw new ArgumentOutOfRangeException("value", $"{value} for mode"), 
			};
			break;
		default:
			Log.Error("Unexpected purpose {purpose}", purpose);
			return;
		}
		_control.Value = value2;
		if (!isInitial)
		{
			PlayClick();
		}
	}

	private void PlayClick()
	{
		if (audioClips.Count > 0)
		{
			AudioClip clip = audioClips[UnityEngine.Random.Range(0, audioClips.Count)];
			IAudioSource audioSource = VirtualAudioSourcePool.Checkout("CTCKnob", clip, loop: false, AudioController.Group.PlayerAction, 5, base.transform, AudioDistance.Nearby);
			if (audioSource == null)
			{
				Debug.LogWarning("Can't play click -- missing VirtualAudioSourcePool?");
				return;
			}
			audioSource.volume = 1f;
			CTCPanelController.ConfigurePanelAudioSource(audioSource);
			audioSource.Play();
			VirtualAudioSourcePool.ReturnAfterFinished(audioSource);
		}
	}

	public static SwitchSetting SwitchSettingFromValue(Value value)
	{
		return (SwitchSetting)value.IntValue;
	}

	public static SignalDirection DirectionFromValue(Value value)
	{
		return (SignalDirection)value.IntValue;
	}

	public void SetSwitchSetting(SwitchSetting switchSetting)
	{
		StateManager.AssertIsHost();
		_keyValueObject[Key] = (int)switchSetting;
	}

	public void SetSignalDirection(SignalDirection direction)
	{
		StateManager.AssertIsHost();
		_keyValueObject[Key] = (int)direction;
	}
}
