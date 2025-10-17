using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UI.Common;

public static class RectTransformSetRect
{
	public static void SetFrame(this RectTransform rectTransform, float x, float y, float w, float h)
	{
		rectTransform.pivot = Vector2.zero;
		rectTransform.offsetMin = Vector2.zero;
		rectTransform.offsetMax = Vector2.zero;
		rectTransform.anchorMin = Vector2.zero;
		rectTransform.anchorMax = Vector2.zero;
		rectTransform.anchoredPosition = new Vector2(x, y);
		rectTransform.sizeDelta = new Vector2(w, h);
	}

	public static void SetFrameFillParent(this RectTransform rectTransform)
	{
		rectTransform.anchorMin = Vector2.zero;
		rectTransform.anchorMax = new Vector2(1f, 1f);
		rectTransform.anchoredPosition = new Vector2(0f, 0f);
		rectTransform.sizeDelta = Vector2.zero;
	}

	public static void SetSizeWithCurrentAnchors(this RectTransform rectTransform, Vector2 size)
	{
		rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
		rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
	}

	public static void DestroyChildren(this RectTransform rectTransform)
	{
		for (int num = rectTransform.transform.childCount - 1; num >= 0; num--)
		{
			Object.Destroy(rectTransform.transform.GetChild(num).gameObject);
		}
	}

	public static void DestroyChildrenExcept(this RectTransform rectTransform, Component except)
	{
		for (int num = rectTransform.transform.childCount - 1; num >= 0; num--)
		{
			Transform child = rectTransform.transform.GetChild(num);
			if ((object)child.gameObject != except.gameObject)
			{
				Object.Destroy(child.gameObject);
			}
		}
	}

	public static void DestroyChildrenExcept(this RectTransform rectTransform, IEnumerable<Component> except)
	{
		for (int num = rectTransform.transform.childCount - 1; num >= 0; num--)
		{
			Transform child = rectTransform.transform.GetChild(num);
			if (!except.Select((Component comp) => comp.gameObject).Contains(child.gameObject))
			{
				Object.Destroy(child.gameObject);
			}
		}
	}
}
