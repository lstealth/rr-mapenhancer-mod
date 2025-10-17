using System;
using UnityEngine;

namespace UI.Builder;

public interface IConfigurableElement
{
	RectTransform RectTransform { get; }

	IConfigurableElement Tooltip(string title, string message);

	IConfigurableElement Tooltip(Func<TooltipInfo> dynamicTooltipInfo);

	IConfigurableElement Disable(bool disable);

	IConfigurableElement ChildWidth(int childIndex, float width);

	IConfigurableElement Width(float width);

	IConfigurableElement Height(float height);
}
