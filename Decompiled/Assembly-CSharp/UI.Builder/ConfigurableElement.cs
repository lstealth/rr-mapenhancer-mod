using System;
using Serilog;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Builder;

internal readonly struct ConfigurableElement : IConfigurableElement
{
	public RectTransform RectTransform { get; }

	public ConfigurableElement(RectTransform rectTransform)
	{
		RectTransform = rectTransform;
	}

	public IConfigurableElement Tooltip(string title, string message)
	{
		RectTransform.Tooltip(title, message);
		return this;
	}

	public IConfigurableElement Tooltip(Func<TooltipInfo> dynamicTooltipInfo)
	{
		RectTransform.Tooltip(dynamicTooltipInfo);
		return this;
	}

	public IConfigurableElement Disable(bool disable)
	{
		Button componentInChildren = RectTransform.GetComponentInChildren<Button>();
		if (componentInChildren != null)
		{
			componentInChildren.interactable = !disable;
		}
		else
		{
			Log.Error("Disable(): Button not found");
		}
		return this;
	}

	public IConfigurableElement ChildWidth(int childIndex, float width)
	{
		((RectTransform)RectTransform.GetChild(childIndex)).Width(width);
		return this;
	}

	public IConfigurableElement Width(float width)
	{
		RectTransform.Width(width);
		return this;
	}

	public IConfigurableElement Height(float height)
	{
		RectTransform.Height(height);
		return this;
	}
}
