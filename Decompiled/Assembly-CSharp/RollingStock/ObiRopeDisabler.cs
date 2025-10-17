using System;
using System.Collections;
using System.Collections.Generic;
using KeyValue.Runtime;
using Obi;
using UnityEngine;

namespace RollingStock;

public class ObiRopeDisabler : MonoBehaviour
{
	public KeyValueObject keyValueObject;

	public string key;

	public float timeout = 5f;

	[Tooltip("Updater we will disable.")]
	public ObiFixedUpdater obiFixedUpdater;

	private readonly HashSet<IDisposable> _observers = new HashSet<IDisposable>();

	private Coroutine _coroutine;

	private void OnEnable()
	{
		_observers.Add(keyValueObject.Observe(key, delegate
		{
			if (_coroutine != null)
			{
				StopCoroutine(_coroutine);
			}
			_coroutine = StartCoroutine(TimeoutCoroutine());
		}));
	}

	private void OnDisable()
	{
		foreach (IDisposable observer in _observers)
		{
			observer.Dispose();
		}
		_observers.Clear();
	}

	private IEnumerator TimeoutCoroutine()
	{
		obiFixedUpdater.enabled = true;
		yield return new WaitForSeconds(timeout);
		obiFixedUpdater.enabled = false;
	}
}
