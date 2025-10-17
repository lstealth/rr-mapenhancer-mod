using System;
using System.Collections;
using Serilog;
using UnityEngine;

namespace Helpers;

public class CoalescingAction : IDisposable
{
	private readonly Func<bool> _readyToWork;

	private readonly Action _work;

	private readonly MonoBehaviour _component;

	private Coroutine _coroutine;

	public CoalescingAction(MonoBehaviour component, Func<bool> readyToWork, Action work)
	{
		_component = component;
		_readyToWork = readyToWork;
		_work = work;
	}

	public void Dispose()
	{
		if (_coroutine != null)
		{
			_component.StopCoroutine(_coroutine);
		}
		_coroutine = null;
	}

	public void RequestRun()
	{
		if (_coroutine == null)
		{
			_coroutine = _component.StartCoroutine(Loop());
		}
	}

	private IEnumerator Loop()
	{
		yield return null;
		WaitForSecondsRealtime wait = new WaitForSecondsRealtime(1f);
		while (true)
		{
			try
			{
				if (_readyToWork())
				{
					break;
				}
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Error while waiting for ready");
				_coroutine = null;
				yield break;
			}
			yield return wait;
		}
		try
		{
			_work();
		}
		catch (Exception exception2)
		{
			Log.Error(exception2, "Error from work action");
		}
		finally
		{
			_coroutine = null;
		}
	}
}
