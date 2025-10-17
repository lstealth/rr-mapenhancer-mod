using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using Network.Messages;
using Serilog;
using UI.Console;
using UnityEngine;

namespace UI.Common;

public class WindowManager : MonoBehaviour
{
	public static WindowManager Shared { get; private set; }

	private Window TopmostShownWindow => EnumerateWindows().FirstOrDefault((Window w) => w.IsShown);

	private void Awake()
	{
		Shared = this;
	}

	private void OnEnable()
	{
		CloseAllWindows();
	}

	private void CloseAllWindows()
	{
		foreach (Window item in EnumerateWindows())
		{
			item.CloseWindow();
		}
	}

	public void CloseTopmostWindow()
	{
		Window topmostShownWindow = TopmostShownWindow;
		if (topmostShownWindow == null)
		{
			Log.Warning("No topmost window");
		}
		else
		{
			topmostShownWindow.HandleRequestCloseWindow();
		}
	}

	public TWindow GetWindow<TWindow>()
	{
		foreach (Transform item in base.transform)
		{
			TWindow component = item.GetComponent<TWindow>();
			if (component != null)
			{
				return component;
			}
		}
		throw new ArgumentException("Couldn't find TWindow");
	}

	private IEnumerable<Window> EnumerateWindows()
	{
		for (int i = base.transform.childCount - 1; i >= 0; i--)
		{
			Window component = base.transform.GetChild(i).GetComponent<Window>();
			if (!(component == null))
			{
				yield return component;
			}
		}
	}

	public Window HitTest(Vector3 mousePosition)
	{
		foreach (Window item in EnumerateWindows())
		{
			if (item.IsShown)
			{
				RectTransform component = item.GetComponent<RectTransform>();
				Vector3 point = component.InverseTransformPoint(mousePosition);
				if (component.rect.Contains(point))
				{
					return item;
				}
			}
		}
		return null;
	}

	public void Present(Alert alert)
	{
		switch (alert.Style)
		{
		case AlertStyle.Toast:
			Toast.Present(alert.Message, (alert.Level != AlertLevel.Error) ? ToastPosition.Bottom : ToastPosition.Middle);
			break;
		case AlertStyle.Console:
			UI.Console.Console.shared.AddLine(alert.Message, new GameDateTime(alert.Timestamp));
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}
}
