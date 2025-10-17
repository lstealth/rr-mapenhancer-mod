using System;
using System.Collections.Generic;
using GalaSoft.MvvmLight.Messaging;
using Serilog;
using UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Builder;

public class UIPanel : IDisposable
{
	private readonly UIBuilderAssets _assets;

	private readonly RectTransform _container;

	private readonly Action<UIPanelBuilder> _buildClosure;

	private readonly HashSet<UIPanel> _children = new HashSet<UIPanel>();

	private UIPanel _parent;

	private int _id;

	private static int _nextId;

	private bool _registeredForEvents;

	private readonly HashSet<IDisposable> _keyChangeObservers = new HashSet<IDisposable>();

	private Timer _timer;

	public event Action OnRebuild;

	public static UIPanel Create(RectTransform container, UIBuilderAssets assets, Action<UIPanelBuilder> closure)
	{
		UIPanel uIPanel = new UIPanel(null, container, assets, closure);
		uIPanel.Rebuild();
		return uIPanel;
	}

	private UIPanel(UIPanel parent, RectTransform container, UIBuilderAssets assets, Action<UIPanelBuilder> closure)
	{
		_parent = parent;
		_assets = assets;
		_container = container;
		_container.DestroyChildren();
		_buildClosure = closure;
		_id = _nextId++;
		if (_container.GetComponent<LayoutGroup>() == null)
		{
			_container.gameObject.AddComponent<VerticalLayoutGroup>();
		}
	}

	public override string ToString()
	{
		return $"UIPanel-{_id}";
	}

	public void Dispose()
	{
		if (_timer != null)
		{
			UnityEngine.Object.DestroyImmediate(_timer);
			_timer = null;
		}
		DisposeChildren();
		_parent = null;
	}

	private void DisposeChildren()
	{
		UnregisterForEvents();
		foreach (UIPanel child in _children)
		{
			child.Dispose();
		}
		_children.Clear();
	}

	public void UnregisterForEvents()
	{
		if (_registeredForEvents)
		{
			Messenger.Default.Unregister(this);
			_registeredForEvents = false;
		}
		foreach (IDisposable keyChangeObserver in _keyChangeObservers)
		{
			keyChangeObserver.Dispose();
		}
		_keyChangeObservers.Clear();
	}

	public void RebuildOnEvent<T>()
	{
		_registeredForEvents = true;
		Messenger.Default.Register(this, delegate(T evt)
		{
			Log.Information("Received {evt}, rebuilding.", evt);
			Rebuild();
		});
	}

	public void AddObserver(IDisposable disposable)
	{
		_keyChangeObservers.Add(disposable);
	}

	public void RebuildOnInterval(float seconds)
	{
		if (_timer == null)
		{
			_timer = _container.gameObject.AddComponent<Timer>();
		}
		_timer.Configure(Rebuild, seconds);
	}

	internal void Rebuild()
	{
		DisposeChildren();
		if (_container == null)
		{
			Log.Warning("Rebuild can't proceed -- _container is null");
			return;
		}
		_container.DestroyChildren();
		_buildClosure(new UIPanelBuilder(_container, _assets, this));
		InvokeOnRebuild();
	}

	private void InvokeOnRebuild()
	{
		this.OnRebuild?.Invoke();
		_parent?.InvokeOnRebuild();
	}

	internal UIPanel AddChild(RectTransform rectTransform, Action<UIPanelBuilder> closure)
	{
		UIPanel uIPanel = new UIPanel(this, rectTransform, _assets, closure);
		_children.Add(uIPanel);
		uIPanel.Rebuild();
		return uIPanel;
	}
}
