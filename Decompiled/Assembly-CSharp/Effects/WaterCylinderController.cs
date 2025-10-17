using System;
using System.Collections;
using KeyValue.Runtime;
using UnityEngine;
using UnityEngine.VFX;

namespace Effects;

public class WaterCylinderController : MonoBehaviour
{
	public MeshRenderer meshRenderer;

	public string key = "key";

	public float speed = 2f;

	[Range(0f, 5f)]
	public float startDelay;

	[Range(0f, 5f)]
	public float stopDelay;

	public VisualEffect sprayEffect;

	private IDisposable _observer;

	private Material _material;

	private Coroutine _coroutine;

	private bool _isFirstValue = true;

	private float _currentValue;

	private float _targetValue;

	private static readonly int FillTop = Shader.PropertyToID("_FillTop");

	private static readonly int FillBottom = Shader.PropertyToID("_FillBottom");

	private static readonly int Speed = Shader.PropertyToID("_Speed");

	private void Awake()
	{
		_material = meshRenderer.material;
	}

	private void OnEnable()
	{
		_isFirstValue = true;
		_observer = GetComponentInParent<KeyValueObject>().Observe(key, PropertyDidChange);
	}

	private void OnDisable()
	{
		_observer?.Dispose();
		_observer = null;
	}

	private void OnDestroy()
	{
		UnityEngine.Object.Destroy(_material);
	}

	private void PropertyDidChange(Value value)
	{
		_targetValue = (value.BoolValue ? 1 : 0);
		float delay = (value.BoolValue ? startDelay : stopDelay);
		int num = (value.BoolValue ? 4 : 20);
		if (_isFirstValue)
		{
			SetValueImmediate(_targetValue);
			_isFirstValue = false;
		}
		else if (_coroutine == null)
		{
			_coroutine = StartCoroutine(UpdateCoroutine(delay, num));
		}
	}

	private IEnumerator UpdateCoroutine(float delay, float lerpSpeed)
	{
		yield return new WaitForSeconds(delay);
		while (Mathf.Abs(_currentValue - _targetValue) > 0.01f)
		{
			SetValueImmediate(Mathf.Lerp(_currentValue, _targetValue, Time.deltaTime * lerpSpeed));
			yield return null;
		}
		SetValueImmediate(_targetValue);
		_coroutine = null;
	}

	private void SetValueImmediate(float value)
	{
		if (value > _currentValue)
		{
			_material.SetFloat(FillTop, 1f);
			_material.SetFloat(FillBottom, Mathf.InverseLerp(0f, 1f, value));
		}
		else
		{
			_material.SetFloat(FillTop, Mathf.InverseLerp(0f, 1f, value));
			_material.SetFloat(FillBottom, 1f);
		}
		_material.SetFloat(Speed, speed);
		sprayEffect.SetFloat("Rate", value * 500f);
		_currentValue = value;
	}
}
