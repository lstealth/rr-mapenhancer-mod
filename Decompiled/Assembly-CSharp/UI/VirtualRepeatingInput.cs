using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UI;

public class VirtualRepeatingInput : IDisposable
{
	private readonly InputAction _action;

	private readonly float _repeatInterval;

	private float _downFrameTime;

	private bool _pressed;

	public VirtualRepeatingInput(InputAction inputAction, float repeatInterval = 0.05f)
	{
		_action = inputAction;
		_repeatInterval = repeatInterval;
		_action.started += OnStarted;
		_action.canceled += OnCanceled;
	}

	public void Dispose()
	{
		_action.started -= OnStarted;
		_action.canceled -= OnCanceled;
	}

	public bool ActiveThisFrame()
	{
		if (!_pressed)
		{
			return false;
		}
		float unscaledTime = Time.unscaledTime;
		if (Math.Abs(_downFrameTime - unscaledTime) < 0.001f)
		{
			return true;
		}
		if (_downFrameTime + _repeatInterval > unscaledTime)
		{
			return false;
		}
		while (_downFrameTime + _repeatInterval <= unscaledTime)
		{
			_downFrameTime += _repeatInterval;
		}
		return true;
	}

	private void OnStarted(InputAction.CallbackContext obj)
	{
		_pressed = true;
		_downFrameTime = Time.unscaledTime;
	}

	private void OnCanceled(InputAction.CallbackContext obj)
	{
		_pressed = false;
	}
}
