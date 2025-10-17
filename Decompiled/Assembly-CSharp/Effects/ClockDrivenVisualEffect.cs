using System;
using UnityEngine;
using UnityEngine.VFX;

namespace Effects;

[RequireComponent(typeof(VisualEffect))]
public class ClockDrivenVisualEffect : MonoBehaviour
{
	[Range(0f, 24f)]
	public float hourOn;

	[Range(0f, 24f)]
	public float hourOff;

	private VisualEffect _visualEffect;

	private IDisposable _scheduleHandle;

	private bool _isOn;

	private void Awake()
	{
		_visualEffect = GetComponent<VisualEffect>();
		SetVisualEffectOn(_isOn);
	}

	private void OnEnable()
	{
		_visualEffect.Stop();
		SetOn(on: false);
		Schedule();
	}

	private void OnDisable()
	{
		_scheduleHandle?.Dispose();
		_scheduleHandle = null;
	}

	private void OnValidate()
	{
		if (Application.isPlaying)
		{
			Schedule();
		}
	}

	private void Schedule()
	{
		_scheduleHandle?.Dispose();
		_scheduleHandle = ClockDriver.Instance.Schedule(hourOn, hourOff, SetOn);
	}

	private void SetOn(bool on)
	{
		if (_visualEffect == null)
		{
			_isOn = on;
			return;
		}
		if (on && !_isOn)
		{
			_visualEffect.Play();
		}
		SetVisualEffectOn(on);
		_isOn = on;
		LeanTween.cancel(base.gameObject);
		if (on)
		{
			return;
		}
		LeanTween.delayedCall(base.gameObject, 30f, (Action)delegate
		{
			if (!_isOn)
			{
				_visualEffect.Stop();
			}
		});
	}

	private void SetVisualEffectOn(bool on)
	{
		if (!(_visualEffect == null))
		{
			_visualEffect.SetBool("Run", on);
		}
	}
}
