using UnityEngine;
using UnityEngine.UI;

namespace UI.Builder;

public static class LayoutGroupFluentExtensions
{
	public static LayoutGroup Padding(this LayoutGroup layoutGroup, RectOffset padding)
	{
		layoutGroup.padding = padding;
		return layoutGroup;
	}

	public static LayoutGroup Padding(this LayoutGroup layoutGroup, int paddingAll)
	{
		layoutGroup.padding = new RectOffset(paddingAll, paddingAll, paddingAll, paddingAll);
		return layoutGroup;
	}
}
