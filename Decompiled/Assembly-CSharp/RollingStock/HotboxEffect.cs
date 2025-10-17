using System;
using System.Collections;
using Game.Messages;
using KeyValue.Runtime;
using Model;
using RollingStock.Steam;
using UnityEngine;
using UnityEngine.VFX;

namespace RollingStock;

public class HotboxEffect : MonoBehaviour
{
	[SerializeField]
	private VisualEffect smokeEffect;

	[SerializeField]
	private Light light;

	private IDisposable _observer;

	private int _index;

	private int _carHash;

	private float _lightIntensity;

	private bool _firstUpdateHotbox = true;

	private Coroutine _coroutine;

	private bool _hotbox;

	private GameObject _effectGameObject;

	private SmokeEffectWrapper _smoke;

	private float _smokeRate0;

	private float _smokeVelocity0;

	private Car _car;

	private void Awake()
	{
		WireUpIfNeeded();
	}

	private void OnEnable()
	{
		_observer = _car.ControlProperties.Observe(PropertyChange.Control.Hotbox, delegate(Value value)
		{
			UpdateForHotbox(value);
		}, callInitial: true);
	}

	private void OnDisable()
	{
		_observer?.Dispose();
		_observer = null;
		_coroutine = null;
	}

	public void Configure(Car car, float axleSeparation, float diameter, int index)
	{
		WireUpIfNeeded();
		_index = index;
		_car = car;
		_carHash = car.id.GetHashCode();
		int num = (((_carHash >> 2) % 2 != 0) ? 1 : (-1));
		_smoke.PositionOffset = new Vector3(0f, 0.2f, 0f);
		smokeEffect.transform.localPosition = new Vector3((float)num * 1.15f, diameter / 2f, axleSeparation / 2f);
		smokeEffect.transform.localRotation = Quaternion.Euler(0f, 0f, -num * 90);
	}

	private void WireUpIfNeeded()
	{
		if (!(_effectGameObject != null))
		{
			_lightIntensity = light.intensity;
			_effectGameObject = smokeEffect.gameObject;
			_smoke = new SmokeEffectWrapper(smokeEffect);
			_smokeRate0 = _smoke.Rate;
			_smokeVelocity0 = _smoke.Velocity;
		}
	}

	private void UpdateForHotbox(bool hotbox)
	{
		bool flag = Mathf.Abs(_carHash % 2) == _index;
		hotbox = hotbox && flag;
		_hotbox = hotbox;
		if (_coroutine != null != hotbox || _firstUpdateHotbox)
		{
			if (hotbox)
			{
				_coroutine = StartCoroutine(Loop());
			}
			else if (_coroutine == null)
			{
				_effectGameObject.SetActive(value: false);
				light.intensity = 0f;
			}
		}
	}

	private IEnumerator Loop()
	{
		_effectGameObject.SetActive(value: true);
		smokeEffect.Play();
		WaitForSeconds waitOneSecond = new WaitForSeconds(1f);
		while (_hotbox)
		{
			float a = Mathf.InverseLerp(0.75f, 0f, _car.Oiled);
			float b = Mathf.InverseLerp(15f, 20f, _car.VelocityMphAbs);
			float t = Mathf.Lerp(a, b, 0.5f);
			_smoke.Rate = Mathf.Lerp(_smokeRate0 * 0.2f, _smokeRate0, t);
			_smoke.Velocity = Mathf.Lerp(_smokeVelocity0 * 0.3f, _smokeVelocity0, t);
			float num = Mathf.Max(a, b);
			if (num > 0.01f)
			{
				AnimateLight(_lightIntensity * num, 0.5f);
			}
			else if (light.enabled)
			{
				AnimateLight(0f, 0.5f);
			}
			yield return waitOneSecond;
		}
		if (light.enabled)
		{
			AnimateLight(0f, 0.5f);
			_smoke.Rate = 0f;
			yield return waitOneSecond;
		}
		smokeEffect.Stop();
		_effectGameObject.SetActive(value: false);
		_coroutine = null;
	}

	private void AnimateLight(float target, float duration)
	{
		if (target > 0.001f)
		{
			light.enabled = true;
		}
		LTSeq lTSeq = LeanTween.sequence();
		lTSeq.append(LeanTween.value(base.gameObject, delegate(float value)
		{
			light.intensity = value;
		}, light.intensity, target, duration).setEaseInOutQuad());
		if (target < 0.001f)
		{
			lTSeq.append(delegate
			{
				light.enabled = false;
			});
		}
	}
}
