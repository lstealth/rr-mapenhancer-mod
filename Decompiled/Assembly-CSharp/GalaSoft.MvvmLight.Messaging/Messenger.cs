using System;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight.Helpers;
using Serilog;

namespace GalaSoft.MvvmLight.Messaging;

public class Messenger : IMessenger
{
	private struct WeakActionAndToken
	{
		public WeakAction Action;

		public object Token;
	}

	private static Messenger _defaultInstance;

	private Dictionary<Type, List<WeakActionAndToken>> _recipientsOfSubclassesAction;

	private Dictionary<Type, List<WeakActionAndToken>> _recipientsStrictAction;

	public static Messenger Default
	{
		get
		{
			if (_defaultInstance == null)
			{
				_defaultInstance = new Messenger();
			}
			return _defaultInstance;
		}
	}

	public static void OverrideDefault(Messenger newMessenger)
	{
		_defaultInstance = newMessenger;
	}

	public static void Reset()
	{
		_defaultInstance = null;
	}

	public virtual void Register<TMessage>(object recipient, Action<TMessage> action)
	{
		Register(recipient, null, receiveDerivedMessagesToo: false, action);
	}

	public virtual void Register<TMessage>(object recipient, bool receiveDerivedMessagesToo, Action<TMessage> action)
	{
		Register(recipient, null, receiveDerivedMessagesToo, action);
	}

	public virtual void Register<TMessage>(object recipient, object token, Action<TMessage> action)
	{
		Register(recipient, token, receiveDerivedMessagesToo: false, action);
	}

	public virtual void Register<TMessage>(object recipient, object token, bool receiveDerivedMessagesToo, Action<TMessage> action)
	{
		Type typeFromHandle = typeof(TMessage);
		Dictionary<Type, List<WeakActionAndToken>> dictionary;
		if (receiveDerivedMessagesToo)
		{
			if (_recipientsOfSubclassesAction == null)
			{
				_recipientsOfSubclassesAction = new Dictionary<Type, List<WeakActionAndToken>>();
			}
			dictionary = _recipientsOfSubclassesAction;
		}
		else
		{
			if (_recipientsStrictAction == null)
			{
				_recipientsStrictAction = new Dictionary<Type, List<WeakActionAndToken>>();
			}
			dictionary = _recipientsStrictAction;
		}
		List<WeakActionAndToken> list;
		if (!dictionary.ContainsKey(typeFromHandle))
		{
			list = new List<WeakActionAndToken>();
			dictionary.Add(typeFromHandle, list);
		}
		else
		{
			list = dictionary[typeFromHandle];
		}
		WeakAction<TMessage> action2 = new WeakAction<TMessage>(recipient, action);
		WeakActionAndToken item = new WeakActionAndToken
		{
			Action = action2,
			Token = token
		};
		list.Add(item);
		Cleanup();
	}

	public virtual void Send<TMessage>(TMessage message)
	{
		SendToTargetOrType(message, null, null);
	}

	public virtual void Send<TMessage, TTarget>(TMessage message)
	{
		SendToTargetOrType(message, typeof(TTarget), null);
	}

	public virtual void Send<TMessage>(TMessage message, object token)
	{
		SendToTargetOrType(message, null, token);
	}

	public virtual void Unregister(object recipient)
	{
		UnregisterFromLists(recipient, _recipientsOfSubclassesAction);
		UnregisterFromLists(recipient, _recipientsStrictAction);
	}

	public virtual void Unregister<TMessage>(object recipient)
	{
		Unregister<TMessage>(recipient, null);
	}

	public virtual void Unregister<TMessage>(object recipient, object token)
	{
		Unregister<TMessage>(recipient, token, null);
	}

	public virtual void Unregister<TMessage>(object recipient, Action<TMessage> action)
	{
		UnregisterFromLists(recipient, action, _recipientsStrictAction);
		UnregisterFromLists(recipient, action, _recipientsOfSubclassesAction);
		Cleanup();
	}

	public virtual void Unregister<TMessage>(object recipient, object token, Action<TMessage> action)
	{
		UnregisterFromLists(recipient, token, action, _recipientsStrictAction);
		UnregisterFromLists(recipient, token, action, _recipientsOfSubclassesAction);
		Cleanup();
	}

	private static void CleanupList(IDictionary<Type, List<WeakActionAndToken>> lists)
	{
		if (lists == null)
		{
			return;
		}
		List<Type> list = new List<Type>();
		foreach (KeyValuePair<Type, List<WeakActionAndToken>> list3 in lists)
		{
			List<WeakActionAndToken> list2 = new List<WeakActionAndToken>();
			foreach (WeakActionAndToken item in list3.Value)
			{
				if (item.Action == null || !item.Action.IsAlive)
				{
					list2.Add(item);
				}
			}
			foreach (WeakActionAndToken item2 in list2)
			{
				list3.Value.Remove(item2);
			}
			if (list3.Value.Count == 0)
			{
				list.Add(list3.Key);
			}
		}
		foreach (Type item3 in list)
		{
			lists.Remove(item3);
		}
	}

	private static bool Implements(Type instanceType, Type interfaceType)
	{
		if (interfaceType == null || instanceType == null)
		{
			return false;
		}
		Type[] interfaces = instanceType.GetInterfaces();
		for (int i = 0; i < interfaces.Length; i++)
		{
			if (interfaces[i] == interfaceType)
			{
				return true;
			}
		}
		return false;
	}

	private static void SendToList<TMessage>(TMessage message, IEnumerable<WeakActionAndToken> list, Type messageTargetType, object token)
	{
		if (list == null)
		{
			return;
		}
		foreach (WeakActionAndToken item in list.Take(list.Count()).ToList())
		{
			if (item.Action is IExecuteWithObject executeWithObject && item.Action.IsAlive && item.Action.Target != null && (messageTargetType == null || item.Action.Target.GetType() == messageTargetType || Implements(item.Action.Target.GetType(), messageTargetType)) && ((item.Token == null && token == null) || (item.Token != null && item.Token.Equals(token))))
			{
				try
				{
					executeWithObject.ExecuteWithObject(message);
				}
				catch (Exception exception)
				{
					Log.Error(exception, $"Exception in SendToList for {message}");
				}
			}
		}
	}

	private static void UnregisterFromLists(object recipient, Dictionary<Type, List<WeakActionAndToken>> lists)
	{
		if (recipient == null || lists == null || lists.Count == 0)
		{
			return;
		}
		lock (lists)
		{
			foreach (Type key in lists.Keys)
			{
				foreach (WeakActionAndToken item in lists[key])
				{
					WeakAction action = item.Action;
					if (action != null && recipient == action.Target)
					{
						action.MarkForDeletion();
					}
				}
			}
		}
	}

	private static void UnregisterFromLists<TMessage>(object recipient, Action<TMessage> action, Dictionary<Type, List<WeakActionAndToken>> lists)
	{
		Type typeFromHandle = typeof(TMessage);
		if (recipient == null || lists == null || lists.Count == 0 || !lists.ContainsKey(typeFromHandle))
		{
			return;
		}
		lock (lists)
		{
			foreach (WeakActionAndToken item in lists[typeFromHandle])
			{
				if (item.Action is WeakAction<TMessage> weakAction && recipient == weakAction.Target && (action == null || action == weakAction.Action))
				{
					item.Action.MarkForDeletion();
				}
			}
		}
	}

	private static void UnregisterFromLists<TMessage>(object recipient, object token, Action<TMessage> action, Dictionary<Type, List<WeakActionAndToken>> lists)
	{
		Type typeFromHandle = typeof(TMessage);
		if (recipient == null || lists == null || lists.Count == 0 || !lists.ContainsKey(typeFromHandle))
		{
			return;
		}
		lock (lists)
		{
			foreach (WeakActionAndToken item in lists[typeFromHandle])
			{
				if (item.Action is WeakAction<TMessage> weakAction && recipient == weakAction.Target && (action == null || action == weakAction.Action) && (token == null || token.Equals(item.Token)))
				{
					item.Action.MarkForDeletion();
				}
			}
		}
	}

	private void Cleanup()
	{
		CleanupList(_recipientsOfSubclassesAction);
		CleanupList(_recipientsStrictAction);
	}

	private void SendToTargetOrType<TMessage>(TMessage message, Type messageTargetType, object token)
	{
		Type typeFromHandle = typeof(TMessage);
		if (_recipientsOfSubclassesAction != null)
		{
			foreach (Type item in _recipientsOfSubclassesAction.Keys.Take(_recipientsOfSubclassesAction.Count()).ToList())
			{
				List<WeakActionAndToken> list = null;
				if (typeFromHandle == item || typeFromHandle.IsSubclassOf(item) || Implements(typeFromHandle, item))
				{
					list = _recipientsOfSubclassesAction[item];
				}
				SendToList(message, list, messageTargetType, token);
			}
		}
		if (_recipientsStrictAction != null && _recipientsStrictAction.ContainsKey(typeFromHandle))
		{
			List<WeakActionAndToken> list2 = _recipientsStrictAction[typeFromHandle];
			SendToList(message, list2, messageTargetType, token);
		}
		Cleanup();
	}
}
