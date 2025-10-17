using System;
using TMPro;
using UI.Tooltips;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Builder;

public static class RectTransformLayoutExtensions
{
	private static LayoutElement GetOrAddLayoutElement(this Component component)
	{
		return component.GetComponent<LayoutElement>() ?? component.gameObject.AddComponent<LayoutElement>();
	}

	public static RectTransform FlexibleWidth(this RectTransform rectTransform, float value = 10000f)
	{
		rectTransform.GetOrAddLayoutElement().flexibleWidth = value;
		return rectTransform;
	}

	public static RectTransform FlexibleHeight(this RectTransform rectTransform, float value = 10000f)
	{
		rectTransform.GetOrAddLayoutElement().flexibleHeight = value;
		return rectTransform;
	}

	public static RectTransform Width(this RectTransform rectTransform, float width)
	{
		LayoutElement orAddLayoutElement = rectTransform.GetOrAddLayoutElement();
		orAddLayoutElement.preferredWidth = width;
		orAddLayoutElement.minWidth = width;
		orAddLayoutElement.flexibleWidth = 0f;
		return rectTransform;
	}

	public static RectTransform Height(this RectTransform rectTransform, float height)
	{
		LayoutElement orAddLayoutElement = rectTransform.GetOrAddLayoutElement();
		orAddLayoutElement.preferredHeight = height;
		orAddLayoutElement.minHeight = height;
		orAddLayoutElement.flexibleHeight = 0f;
		return rectTransform;
	}

	public static RectTransform ChildAlignment(this RectTransform rectTransform, TextAnchor alignment)
	{
		rectTransform.GetComponent<LayoutGroup>().childAlignment = alignment;
		return rectTransform;
	}

	public static RectTransform Tooltip(this RectTransform rectTransform, string title, string message)
	{
		(rectTransform.GetComponent<UITooltipProvider>() ?? rectTransform.gameObject.AddComponent<UITooltipProvider>()).TooltipInfo = new TooltipInfo(title, message);
		return rectTransform;
	}

	public static RectTransform Tooltip(this RectTransform rectTransform, Func<TooltipInfo> dynamicTooltipInfo)
	{
		(rectTransform.GetComponent<UITooltipProvider>() ?? rectTransform.gameObject.AddComponent<UITooltipProvider>()).DynamicTooltipInfo = dynamicTooltipInfo;
		return rectTransform;
	}

	public static void SetTextMarginsTop(this RectTransform rectTransform, float padding)
	{
		TMP_Text[] componentsInChildren = rectTransform.GetComponentsInChildren<TMP_Text>();
		foreach (TMP_Text tMP_Text in componentsInChildren)
		{
			Transform parent = tMP_Text.transform.parent;
			if (!(parent.GetComponent<HorizontalLayoutGroup>() == null) && !(parent.GetComponent<Button>() != null) && !(parent.GetComponent<TMP_Dropdown>() != null))
			{
				tMP_Text.margin = new Vector4(0f, padding, 0f, 0f);
			}
		}
	}

	public static RectTransform HorizontalTextAlignment(this RectTransform rectTransform, HorizontalAlignmentOptions horizontalAlignment)
	{
		TMP_Text componentInChildren = rectTransform.GetComponentInChildren<TMP_Text>();
		if (componentInChildren != null)
		{
			componentInChildren.horizontalAlignment = horizontalAlignment;
		}
		return rectTransform;
	}

	public static RectTransform VerticalTextAlignment(this RectTransform rectTransform, VerticalAlignmentOptions verticalAlignment)
	{
		TMP_Text componentInChildren = rectTransform.GetComponentInChildren<TMP_Text>();
		if (componentInChildren != null)
		{
			componentInChildren.verticalAlignment = verticalAlignment;
		}
		return rectTransform;
	}

	public static RectTransform TextWrap(this RectTransform rectTransform, TextOverflowModes overflowMode, TextWrappingModes textWrappingMode)
	{
		TMP_Text componentInChildren = rectTransform.GetComponentInChildren<TMP_Text>();
		if (componentInChildren != null)
		{
			componentInChildren.overflowMode = overflowMode;
			componentInChildren.textWrappingMode = textWrappingMode;
		}
		return rectTransform;
	}
}
