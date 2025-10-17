using System;

namespace GalaSoft.MvvmLight.Helpers;

public class WeakAction
{
	private readonly Action _action;

	private WeakReference _reference;

	public Action Action => _action;

	public bool IsAlive
	{
		get
		{
			if (_reference == null)
			{
				return false;
			}
			return _reference.IsAlive;
		}
	}

	public object Target
	{
		get
		{
			if (_reference == null)
			{
				return null;
			}
			return _reference.Target;
		}
	}

	public WeakAction(object target, Action action)
	{
		_reference = new WeakReference(target);
		_action = action;
	}

	public void Execute()
	{
		if (_action != null && IsAlive)
		{
			_action();
		}
	}

	public void MarkForDeletion()
	{
		_reference = null;
	}
}
public class WeakAction<T> : WeakAction, IExecuteWithObject
{
	private readonly Action<T> _action;

	public new Action<T> Action => _action;

	public WeakAction(object target, Action<T> action)
		: base(target, null)
	{
		_action = action;
	}

	public new void Execute()
	{
		if (_action != null && base.IsAlive)
		{
			_action(default(T));
		}
	}

	public void Execute(T parameter)
	{
		if (_action != null && base.IsAlive)
		{
			_action(parameter);
		}
	}

	public void ExecuteWithObject(object parameter)
	{
		T parameter2 = (T)parameter;
		Execute(parameter2);
	}
}
