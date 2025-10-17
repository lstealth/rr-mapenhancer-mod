using System.Collections;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Serilog;
using UnityEngine;

namespace Game.State;

public class TimeObserver : MonoBehaviour
{
	private Coroutine _coroutine;

	private GameDateTime _lastTime;

	public void StartObservering()
	{
		_coroutine = StartCoroutine(ObserveTime());
		Messenger.Default.Register<TimeAdvanced>(this, delegate
		{
			CheckForChange();
		});
	}

	public void StopObserving()
	{
		if (_coroutine != null)
		{
			StopCoroutine(_coroutine);
		}
		_coroutine = null;
		Messenger.Default.Unregister(this);
	}

	private IEnumerator ObserveTime()
	{
		WaitForSecondsRealtime wait = new WaitForSecondsRealtime(1f);
		_lastTime = TimeWeather.Now;
		Log.Information("TimeObserver start with {lastTime}", _lastTime);
		while (true)
		{
			yield return wait;
			CheckForChange();
		}
	}

	private void CheckForChange()
	{
		GameDateTime now = TimeWeather.Now;
		if (now.Day != _lastTime.Day)
		{
			Messenger.Default.Send(default(TimeDayDidChange));
		}
		if (Mathf.FloorToInt(now.Hours) != Mathf.FloorToInt(_lastTime.Hours))
		{
			Messenger.Default.Send(default(TimeHourDidChange));
		}
		if (Mathf.FloorToInt(now.Minutes) != Mathf.FloorToInt(_lastTime.Minutes))
		{
			Messenger.Default.Send(default(TimeMinuteDidChange));
		}
		_lastTime = now;
	}
}
