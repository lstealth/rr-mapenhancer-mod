using System;
using Helpers;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI;

public class DraggablePanel : MonoBehaviour, IDragHandler, IEventSystemHandler
{
	[SerializeField]
	private RectTransform panel;

	private Canvas _canvas;

	public event Action<Vector2> OnPanelDragged;

	private void Awake()
	{
		_canvas = GetComponentInParent<Canvas>();
	}

	public void OnDrag(PointerEventData eventData)
	{
		Vector2 vector = (panel.anchoredPosition + eventData.delta / _canvas.scaleFactor).Round();
		panel.anchoredPosition = vector;
		this.OnPanelDragged?.Invoke(vector);
	}
}
