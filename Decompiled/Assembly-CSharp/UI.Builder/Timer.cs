using System;
using System.Collections;
using Serilog;
using UnityEngine;

namespace UI.Builder;

public class Timer : MonoBehaviour
{
	private Coroutine _coroutine;

	private Action _action;

	private float _interval;

	private void OnEnable()
	{
		RestartCoroutine();
	}

	private void OnDisable()
	{
		if (_coroutine != null)
		{
			StopCoroutine(_coroutine);
		}
		_coroutine = null;
	}

	private IEnumerator UpdateCoroutine()
	{
		while (true)
		{
			yield return new WaitForSecondsRealtime(_interval);
			try
			{
				_action();
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Error during timer loop");
			}
		}
	}

	public void Configure(Action action, float interval)
	{
		_action = action;
		_interval = interval;
		if (base.isActiveAndEnabled)
		{
			RestartCoroutine();
		}
	}

	private void RestartCoroutine()
	{
		if (_coroutine != null)
		{
			StopCoroutine(_coroutine);
		}
		_coroutine = StartCoroutine(UpdateCoroutine());
	}
}
