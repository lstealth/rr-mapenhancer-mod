using System;
using System.Collections;
using Serilog;
using UnityEngine;

namespace Helpers;

public class CoroutineKeepalive
{
	private Coroutine _coroutine;

	private MonoBehaviour _mb;

	private float _lastReset;

	private readonly float _timeout;

	private Action _action;

	private readonly bool _scaledTime;

	private float Now
	{
		get
		{
			if (!_scaledTime)
			{
				return Time.unscaledTime;
			}
			return Time.time;
		}
	}

	public CoroutineKeepalive(float timeout, bool scaledTime)
	{
		_timeout = timeout;
		_scaledTime = scaledTime;
	}

	public void Start(MonoBehaviour monoBehaviour, Action keepaliveTimedOut)
	{
		_action = keepaliveTimedOut;
		StillAlive();
		_mb = monoBehaviour;
		_coroutine = _mb.StartCoroutine(Keepalive());
	}

	public void Stop()
	{
		_action = null;
		if (_mb != null && _coroutine != null)
		{
			_mb.StopCoroutine(_coroutine);
			_coroutine = null;
		}
	}

	public void StillAlive()
	{
		_lastReset = Now;
	}

	private IEnumerator Keepalive()
	{
		WaitForSecondsRealtime wait = new WaitForSecondsRealtime(1f);
		while (true)
		{
			yield return wait;
			if (Now - _lastReset > _timeout)
			{
				try
				{
					_action?.Invoke();
				}
				catch (Exception exception)
				{
					Log.Error(exception, "Exception firing keepalive action");
				}
				StillAlive();
			}
		}
	}
}
