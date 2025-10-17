using System;
using System.Collections.Generic;
using Effects;
using KeyValue.Runtime;
using UnityEngine;

namespace RollingStock;

public class LocomotiveLightingController : MonoBehaviour
{
	public string key;

	public List<HeadlightController> headlights;

	private IDisposable _keyValueObserver;

	private void Awake()
	{
		SetState(HeadlightController.State.Off, HeadlightController.State.Off);
	}

	private void OnEnable()
	{
		_keyValueObserver = this.ObserveKeyValueDelayed(key, KeyDidChange);
	}

	private void OnDisable()
	{
		_keyValueObserver?.Dispose();
	}

	private void KeyDidChange(Value value)
	{
		var (forwardState, reverseState) = HeadlightStateLogic.StatesFromInt(value.IntValue);
		SetState(forwardState, reverseState);
	}

	private void SetState(HeadlightController.State forwardState, HeadlightController.State reverseState)
	{
		if (headlights == null)
		{
			return;
		}
		foreach (HeadlightController headlight in headlights)
		{
			HeadlightController current;
			HeadlightController headlightController = (current = headlight);
			current.state = headlightController.Direction switch
			{
				HeadlightController.HeadlightDirection.Forward => forwardState, 
				HeadlightController.HeadlightDirection.Reverse => reverseState, 
				_ => throw new ArgumentOutOfRangeException(), 
			};
		}
	}
}
