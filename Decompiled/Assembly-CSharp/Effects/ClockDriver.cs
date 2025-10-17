using System;
using System.Collections;
using System.Collections.Generic;
using Game;
using Serilog;
using UnityEngine;

namespace Effects;

public class ClockDriver : MonoBehaviour
{
	private class Item
	{
		private readonly float _onTime;

		private readonly float _offTime;

		public readonly Action<bool> Action;

		public bool IsOn;

		public Item(float onTime, float offTime, Action<bool> action)
		{
			_onTime = onTime;
			_offTime = offTime;
			Action = action;
		}

		public bool ShouldBeOn(float hourOfDay)
		{
			if (_onTime < _offTime)
			{
				if (_onTime < hourOfDay)
				{
					return hourOfDay < _offTime;
				}
				return false;
			}
			if (!(hourOfDay < _offTime))
			{
				return _onTime < hourOfDay;
			}
			return true;
		}
	}

	private class Handle : IDisposable
	{
		public Action DisposeAction;

		public void Dispose()
		{
			DisposeAction();
		}
	}

	private Coroutine _coroutine;

	private readonly HashSet<Item> _items = new HashSet<Item>();

	public static ClockDriver Instance { get; private set; }

	private float HourOfDay => TimeWeather.Now.Hours;

	private void Awake()
	{
		Instance = this;
	}

	private void OnEnable()
	{
		_coroutine = StartCoroutine(UpdateLoop());
	}

	private void OnDisable()
	{
		if (_coroutine != null)
		{
			StopCoroutine(_coroutine);
		}
		_coroutine = null;
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public IDisposable Schedule(float onTime, float offTime, Action<bool> action)
	{
		Item item = new Item(onTime, offTime, action);
		item.IsOn = item.ShouldBeOn(HourOfDay);
		action(item.IsOn);
		_items.Add(item);
		return new Handle
		{
			DisposeAction = delegate
			{
				_items.Remove(item);
			}
		};
	}

	private IEnumerator UpdateLoop()
	{
		WaitForSecondsRealtime wait = new WaitForSecondsRealtime(0.5f);
		HashSet<Item> needsToggle = new HashSet<Item>();
		while (true)
		{
			float hourOfDay = HourOfDay;
			foreach (Item item in _items)
			{
				if (item.ShouldBeOn(hourOfDay) != item.IsOn)
				{
					needsToggle.Add(item);
				}
			}
			foreach (Item item2 in needsToggle)
			{
				bool obj = (item2.IsOn = !item2.IsOn);
				try
				{
					item2.Action(obj);
				}
				catch (Exception exception)
				{
					Log.Error(exception, "Exception while invoking action");
				}
			}
			needsToggle.Clear();
			yield return wait;
		}
	}
}
