using UnityEngine;
using UnityEngine.UI;

namespace UI.Builder;

public static class LayoutGroupExtensions
{
	public static HorizontalLayoutGroup Spacing(this HorizontalLayoutGroup group, float spacing)
	{
		group.spacing = spacing;
		return group;
	}

	public static HorizontalLayoutGroup Padding(this HorizontalLayoutGroup group, int padding)
	{
		group.padding = new RectOffset(padding, padding, padding, padding);
		return group;
	}

	public static HorizontalLayoutGroup PreferredHeight(this HorizontalLayoutGroup group, int height)
	{
		(group.gameObject.GetComponent<LayoutElement>() ?? group.gameObject.AddComponent<LayoutElement>()).preferredHeight = height;
		return group;
	}
}
