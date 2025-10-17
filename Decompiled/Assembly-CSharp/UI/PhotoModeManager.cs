using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI;

public class PhotoModeManager : MonoBehaviour
{
	public List<Canvas> canvases = new List<Canvas>();

	public bool on;

	private readonly HashSet<Canvas> _disabledCanvases = new HashSet<Canvas>();

	private readonly HashSet<GraphicRaycaster> _disabledRaycasters = new HashSet<GraphicRaycaster>();

	private void Update()
	{
		if (GameInput.shared.InputTogglePhotoMode)
		{
			Toggle();
		}
	}

	private void Toggle()
	{
		on = !on;
		if (on)
		{
			foreach (Canvas canvase in canvases)
			{
				if (!canvase.enabled)
				{
					continue;
				}
				_disabledCanvases.Add(canvase);
				canvase.enabled = false;
				GraphicRaycaster[] componentsInChildren = canvase.GetComponentsInChildren<GraphicRaycaster>();
				foreach (GraphicRaycaster graphicRaycaster in componentsInChildren)
				{
					if (graphicRaycaster.enabled)
					{
						graphicRaycaster.enabled = false;
						_disabledRaycasters.Add(graphicRaycaster);
					}
				}
			}
			GameInput.RegisterEscapeHandler(GameInput.EscapeHandler.Transient, delegate
			{
				if (on)
				{
					Toggle();
					return true;
				}
				return false;
			});
			return;
		}
		foreach (Canvas canvase2 in canvases)
		{
			if (_disabledCanvases.Contains(canvase2))
			{
				canvase2.enabled = true;
			}
		}
		foreach (GraphicRaycaster disabledRaycaster in _disabledRaycasters)
		{
			disabledRaycaster.enabled = true;
		}
		_disabledCanvases.Clear();
		_disabledRaycasters.Clear();
		GameInput.UnregisterEscapeHandler(GameInput.EscapeHandler.Transient);
	}
}
