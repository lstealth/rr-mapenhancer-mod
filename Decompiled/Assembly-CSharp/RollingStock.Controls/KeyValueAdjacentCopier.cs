using System;
using System.Collections;
using System.Collections.Generic;
using KeyValue.Runtime;
using Model;
using Serilog;
using UnityEngine;

namespace RollingStock.Controls;

public class KeyValueAdjacentCopier : MonoBehaviour
{
	public List<string> keys = new List<string>();

	[Tooltip("End to copy from.")]
	public Car.End end;

	private List<IDisposable> _subscriptions = new List<IDisposable>();

	private Car _car;

	private string _adjacentId;

	private void Start()
	{
		StartCoroutine(CheckForChanges());
	}

	private void OnDestroy()
	{
		UnsubscribeFromAll();
	}

	private IEnumerator CheckForChanges()
	{
		WaitForSeconds wait = new WaitForSeconds(0.5f);
		while (_car == null)
		{
			yield return wait;
			if (_car == null)
			{
				_car = GetComponentInParent<Car>();
			}
			_ = _car == null;
		}
		while (true)
		{
			Car adjacent;
			if (_car.set == null)
			{
				yield return wait;
			}
			else if (!TryGetAdjacentCar(out adjacent))
			{
				if (!string.IsNullOrEmpty(_adjacentId))
				{
					UnsubscribeFromAll();
					_adjacentId = null;
				}
				yield return wait;
			}
			else if (_adjacentId == adjacent.id)
			{
				yield return wait;
			}
			else
			{
				_adjacentId = adjacent.id;
				SetupObservers(adjacent);
				yield return wait;
			}
		}
	}

	private bool TryGetAdjacentCar(out Car adjacent)
	{
		return _car.set.TryGetAdjacentCar(_car, _car.EndToLogical(end), out adjacent);
	}

	private void SetupObservers(Car adjacent)
	{
		UnsubscribeFromAll();
		KeyValueObject carObj = _car.KeyValueObject;
		KeyValueObject keyValueObject = adjacent.KeyValueObject;
		foreach (string key in keys)
		{
			_subscriptions.Add(keyValueObject.Observe(key, delegate(Value value)
			{
				Log.Debug("Observed {key} on {adjacent}", key, adjacent.name);
				carObj[key] = value;
			}));
		}
	}

	private void UnsubscribeFromAll()
	{
		foreach (IDisposable subscription in _subscriptions)
		{
			subscription.Dispose();
		}
		_subscriptions.Clear();
	}
}
