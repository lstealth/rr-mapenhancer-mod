using System;
using System.Collections;
using System.Collections.Generic;
using Game.State;
using KeyValue.Runtime;
using Serilog;
using UnityEngine;

namespace RollingStock;

public class CarLoaderSequencer : MonoBehaviour
{
	public KeyValueObject keyValueObject;

	[Tooltip("Read. When true, will move toward a 'can load' state.")]
	public string readWantsLoadingKey = "request";

	[Tooltip("Read. When true, loading is actually in progress.")]
	public string readIsLoadingKey = "isLoading";

	[Tooltip("Written. Set to true when the loader may load.")]
	public string writeCanLoadKey = "canLoad";

	[Tooltip("Written to true to start an animation into position for loading.")]
	public string writePrepareLoadKey = "prepareLoad";

	[Tooltip("Written to true to start an animation of actually loading.")]
	public string writeAnimateLoadKey = "animateLoad";

	public float prepareLoadingDelay = 2f;

	public float startLoadingDelay = 2f;

	public float stopLoadingDelay = 2f;

	public float cleanupLoadingDelay = 2f;

	public bool logStateChanges;

	private readonly HashSet<IDisposable> _observers = new HashSet<IDisposable>();

	private Coroutine _loopCoroutine;

	private bool WantsLoading => keyValueObject[readWantsLoadingKey].BoolValue;

	private bool IsLoading => keyValueObject[readIsLoadingKey].BoolValue;

	private bool CanLoad
	{
		get
		{
			return keyValueObject[writeCanLoadKey].BoolValue;
		}
		set
		{
			keyValueObject[writeCanLoadKey] = Value.Bool(value);
		}
	}

	private bool PrepareLoad
	{
		get
		{
			return keyValueObject[writePrepareLoadKey].BoolValue;
		}
		set
		{
			keyValueObject[writePrepareLoadKey] = Value.Bool(value);
		}
	}

	private bool AnimateLoad
	{
		get
		{
			return keyValueObject[writeAnimateLoadKey].BoolValue;
		}
		set
		{
			keyValueObject[writeAnimateLoadKey] = Value.Bool(value);
		}
	}

	private void OnEnable()
	{
		if (!StateManager.IsHost)
		{
			return;
		}
		_observers.Add(keyValueObject.Observe(readWantsLoadingKey, delegate
		{
			if (_loopCoroutine == null)
			{
				_loopCoroutine = StartCoroutine(Loop());
			}
		}));
	}

	private void OnDisable()
	{
		foreach (IDisposable observer in _observers)
		{
			observer.Dispose();
		}
		_observers.Clear();
		if (_loopCoroutine != null)
		{
			StopCoroutine(_loopCoroutine);
		}
		_loopCoroutine = null;
	}

	private void LogState(string message)
	{
		if (logStateChanges)
		{
			Log.Information("CarLoaderSequencer {id}: {message}", keyValueObject.RegisteredId ?? base.name, message);
		}
	}

	private IEnumerator Loop()
	{
		LogState("Loop start");
		WaitForSeconds waitForChange = new WaitForSeconds(0.2f);
		do
		{
			PrepareLoad = false;
			AnimateLoad = false;
			if (WantsLoading && !PrepareLoad)
			{
				LogState("PrepareLoad = true");
				PrepareLoad = true;
				yield return new WaitForSeconds(prepareLoadingDelay);
			}
			LogState("CanLoad = true");
			CanLoad = true;
			while (true)
			{
				LogState("Wait for IsLoading...");
				while (WantsLoading && !IsLoading)
				{
					yield return waitForChange;
				}
				if (!WantsLoading)
				{
					break;
				}
				LogState("AnimateLoad = true");
				AnimateLoad = true;
				yield return new WaitForSeconds(startLoadingDelay);
				LogState("Wait for !IsLoading...");
				while (WantsLoading && IsLoading)
				{
					yield return waitForChange;
				}
				LogState("AnimateLoad = false");
				AnimateLoad = false;
				yield return new WaitForSeconds(stopLoadingDelay);
			}
			LogState("Cancel");
			CanLoad = false;
			PrepareLoad = false;
			yield return new WaitForSeconds(cleanupLoadingDelay);
			LogState("Loop complete");
		}
		while (WantsLoading);
		_loopCoroutine = null;
	}
}
