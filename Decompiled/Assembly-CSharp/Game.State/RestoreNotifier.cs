using System;
using System.Collections.Generic;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Serilog;
using UnityEngine;

namespace Game.State;

public class RestoreNotifier : MonoBehaviour
{
	private enum State
	{
		Pending,
		InProgress,
		Complete
	}

	private struct Entry
	{
		public readonly int Priority;

		public readonly object Observer;

		public readonly Action Action;

		public Entry(int priority, object observer, Action action)
		{
			Priority = priority;
			Observer = observer;
			Action = action;
		}
	}

	private State _state;

	private readonly List<Entry> _pending = new List<Entry>();

	public bool HasRestored
	{
		get
		{
			State state = _state;
			return state == State.InProgress || state == State.Complete;
		}
	}

	public static RestoreNotifier Shared { get; private set; }

	public static void Initialize()
	{
		Shared = UnityEngine.Object.FindObjectOfType<RestoreNotifier>();
	}

	public static void Deinitialize()
	{
		if (Shared != null)
		{
			Shared._state = State.Pending;
		}
		Shared = null;
	}

	private void LateUpdate()
	{
		if (_state == State.Complete && _pending.Count > 0)
		{
			NotifyPending();
		}
	}

	public void NotifyDidRestore()
	{
		_state = State.InProgress;
		NotifyPending();
		Messenger.Default.Send(default(PropertiesDidRestore));
		_state = State.Complete;
	}

	private void NotifyPending()
	{
		while (_pending.Count > 0)
		{
			Entry entry = _pending[0];
			_pending.RemoveAt(0);
			try
			{
				entry.Action();
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Error during DidRestore of {observer}", entry.Observer);
			}
		}
	}

	public void RegisterForRestore(int priority, object observer, Action action)
	{
		Entry item = new Entry(priority, observer, action);
		for (int i = 0; i < _pending.Count; i++)
		{
			int priority2 = _pending[i].Priority;
			if (priority > priority2)
			{
				_pending.Insert(i, item);
				return;
			}
		}
		_pending.Add(item);
	}

	public void Unregister(object observer)
	{
		for (int num = _pending.Count - 1; num >= 0; num--)
		{
			if (_pending[num].Observer == observer)
			{
				_pending.RemoveAt(num);
			}
		}
	}
}
