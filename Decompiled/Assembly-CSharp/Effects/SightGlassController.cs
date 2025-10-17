using System;
using System.Collections;
using Helpers;
using KeyValue.Runtime;
using Model;
using Model.Ops;
using UnityEngine;

namespace Effects;

public class SightGlassController : MonoBehaviour
{
	[SerializeField]
	private MeshRenderer waterRenderer;

	[Header("Spring Config")]
	[Range(0f, 10f)]
	public float targetForce = 1.5f;

	[Range(0f, 1f)]
	public float springStiffness = 0.02f;

	[Range(0f, 1f)]
	public float springDamping = 0.99f;

	[Range(0f, 10f)]
	public float springMass = 1f;

	private Car _waterCar;

	private static readonly int Level = Shader.PropertyToID("_Level");

	private IDisposable _fuelObserver;

	private float _targetLevel;

	private float _lastSetLevel;

	private bool _hasSetMaterial;

	private Coroutine _coroutine;

	private Material _material;

	private float _velocity;

	private void Awake()
	{
		_material = waterRenderer.CreateUniqueMaterial();
	}

	private void OnEnable()
	{
		_hasSetMaterial = false;
		FindWaterCar();
	}

	private void OnDisable()
	{
		_fuelObserver?.Dispose();
		_fuelObserver = null;
	}

	private void OnDestroy()
	{
		UnityEngine.Object.Destroy(_material);
		_material = null;
	}

	private void FindWaterCar()
	{
		SteamLocomotive componentInParent = GetComponentInParent<SteamLocomotive>();
		if (componentInParent == null)
		{
			return;
		}
		Car car = componentInParent.FuelCar();
		if (car == null || !car.GetLoadInfo("water", out var slotIndex).HasValue)
		{
			return;
		}
		float capacity = car.Definition.LoadSlots[slotIndex].MaximumCapacity;
		string key = CarExtensions.KeyForLoadInfoSlot(slotIndex);
		_fuelObserver = car.KeyValueObject.Observe(key, delegate(Value value)
		{
			CarLoadInfo? carLoadInfo = CarLoadInfo.FromPropertyValue(value);
			if (carLoadInfo.HasValue)
			{
				CarLoadInfo value2 = carLoadInfo.Value;
				LoadValueDidChange(value2.Quantity / capacity);
			}
		});
	}

	private void LoadValueDidChange(float level)
	{
		if (!_hasSetMaterial || !(Mathf.Abs(_targetLevel - level) < 0.01f))
		{
			_targetLevel = level;
			if (!_hasSetMaterial)
			{
				SetMaterialLevel(level);
				_velocity = 0f;
				_hasSetMaterial = true;
			}
			else if (_coroutine == null)
			{
				_coroutine = StartCoroutine(UpdateCoroutine());
			}
		}
	}

	private IEnumerator UpdateCoroutine()
	{
		while (Mathf.Abs(_lastSetLevel - _targetLevel) > 0.001f || Mathf.Abs(_velocity) > 0.01f)
		{
			float deltaTime = Time.deltaTime;
			float num = (_targetLevel - _lastSetLevel) * targetForce;
			float lastSetLevel = _lastSetLevel;
			float num2 = ((0f - springStiffness) * lastSetLevel + (0f - springDamping) * _velocity + num) / springMass;
			_velocity += num2 * deltaTime;
			lastSetLevel += _velocity * deltaTime;
			SetMaterialLevel(lastSetLevel);
			yield return null;
		}
		_coroutine = null;
	}

	private void SetMaterialLevel(float level)
	{
		_lastSetLevel = Mathf.Clamp01(level);
		_material.SetFloat(Level, level);
	}
}
