using System;
using System.Collections;
using System.Collections.Generic;
using Helpers.Animation;
using KeyValue.Runtime;
using Model;
using Model.Definition.Data;
using Model.Ops;
using UnityEngine;

namespace RollingStock.Controls;

public class CarLoadAnimator : MonoBehaviour
{
	[Tooltip("Which car load slot this object represents.")]
	public int slot;

	[Tooltip("If the load id matches, this load will be shown.")]
	public List<string> loadIdentifiers;

	[Tooltip("Object to toggle when load is present.")]
	public GameObject carLoadGameObject;

	public Animator animator;

	public AnimationClip animationClip;

	public bool clipStartsFull;

	private PlayableHandle _clipPlayable;

	private bool _isInitial = true;

	private CarLoadInfo? _target;

	private Coroutine _updateCoroutine;

	private IDisposable _observer;

	private KeyValueObject _keyValueObject;

	private Car _car;

	private string LoadKey => CarExtensions.KeyForLoadInfoSlot(slot);

	private CarLoadInfo? CurrentCarLoadInfo => CarLoadInfo.FromPropertyValue(_keyValueObject[LoadKey]);

	private void Awake()
	{
		if (carLoadGameObject != null)
		{
			carLoadGameObject.SetActive(value: false);
		}
	}

	private void Start()
	{
		if (animationClip != null)
		{
			_clipPlayable = animator.PlayableGraphAdapter().AddPlayable(animationClip);
			_clipPlayable.Pause();
		}
		_keyValueObject = GetComponentInParent<KeyValueObject>();
		_observer = _keyValueObject.Observe(LoadKey, PropertyChanged);
	}

	private void OnDestroy()
	{
		_observer.Dispose();
		_clipPlayable?.Dispose();
	}

	private void OnValidate()
	{
		if (animator == null)
		{
			animator = GetComponent<Animator>();
		}
	}

	private void PropertyChanged(Value value)
	{
		CarLoadInfo? carLoadInfo = CarLoadInfo.FromPropertyValue(value);
		if (!carLoadInfo.HasValue || loadIdentifiers.Contains(carLoadInfo.Value.LoadId))
		{
			if (carLoadGameObject != null)
			{
				carLoadGameObject.SetActive(value: false);
			}
			StartMoveToTarget();
		}
		else
		{
			bool active = carLoadInfo.Value.Quantity > 0f;
			if (carLoadGameObject != null)
			{
				carLoadGameObject.SetActive(active);
			}
			StartMoveToTarget();
		}
		_isInitial = false;
	}

	private void StartMoveToTarget()
	{
		if (_clipPlayable != null)
		{
			_clipPlayable.Speed = 0f;
			_clipPlayable.Play();
			if (_updateCoroutine == null)
			{
				_updateCoroutine = StartCoroutine(MoveToTarget());
			}
		}
	}

	private IEnumerator MoveToTarget()
	{
		if (_clipPlayable != null)
		{
			float targetTime = TargetTime();
			if (_isInitial)
			{
				_clipPlayable.Time = targetTime;
			}
			else
			{
				while (Math.Abs(_clipPlayable.Time - targetTime) > 0.01f)
				{
					targetTime = TargetTime();
					_clipPlayable.Time = Mathf.MoveTowards(_clipPlayable.Time, targetTime, Time.deltaTime);
					yield return null;
				}
			}
		}
		_updateCoroutine = null;
	}

	private float TargetTime()
	{
		if (animationClip == null)
		{
			return 0f;
		}
		CarLoadInfo? currentCarLoadInfo = CurrentCarLoadInfo;
		float num = (currentCarLoadInfo.HasValue ? CalculatePercent(currentCarLoadInfo.Value) : 0f);
		if (clipStartsFull)
		{
			num = 1f - num;
		}
		return num * animationClip.length;
	}

	private float CalculatePercent(CarLoadInfo info)
	{
		if ((object)_car == null)
		{
			_car = GetComponentInParent<Car>();
		}
		if (_car == null)
		{
			return 0f;
		}
		return CalculatePercent(_car, info);
	}

	public static float CalculatePercent(Car car, CarLoadInfo info)
	{
		foreach (LoadSlot loadSlot in car.Definition.LoadSlots)
		{
			if (loadSlot.LoadRequirementsMatch(info.LoadId))
			{
				return Mathf.Clamp01(info.Quantity / loadSlot.MaximumCapacity);
			}
		}
		return 0f;
	}
}
