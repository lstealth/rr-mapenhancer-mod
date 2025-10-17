using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.CarEditor;

public class SliderArea : MonoBehaviour, IBeginDragHandler, IEventSystemHandler, IDragHandler, IEndDragHandler
{
	public Action<float> onValueChanged;

	public void OnBeginDrag(PointerEventData eventData)
	{
	}

	public void OnDrag(PointerEventData eventData)
	{
		float x = eventData.delta.x;
		float obj = Mathf.Sign(x) * Mathf.Pow(x, 2f) / 1000f;
		onValueChanged?.Invoke(obj);
	}

	public void OnEndDrag(PointerEventData eventData)
	{
	}
}
