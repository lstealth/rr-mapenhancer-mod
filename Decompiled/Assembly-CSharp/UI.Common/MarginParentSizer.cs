using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Common;

[ExecuteInEditMode]
[RequireComponent(typeof(ContentSizeFitter))]
[RequireComponent(typeof(TextMeshProUGUI))]
public class MarginParentSizer : UIBehaviour
{
	public int horizontalMargin;

	public int verticalMargin;

	public bool vertical = true;

	public bool horizontal = true;

	public RectTransform ParentRectTransform => (RectTransform)base.transform.parent;

	public RectTransform RectTransform => (RectTransform)base.transform;

	protected override void OnRectTransformDimensionsChange()
	{
		base.OnRectTransformDimensionsChange();
		Vector3 vector = RectTransform.sizeDelta;
		if (!vertical)
		{
			vector.y = ParentRectTransform.sizeDelta.y;
		}
		else
		{
			vector.y = RectTransform.sizeDelta.y + (float)(verticalMargin * 2);
		}
		if (!horizontal)
		{
			vector.x = ParentRectTransform.sizeDelta.x;
		}
		else
		{
			vector.x = RectTransform.sizeDelta.x + (float)(horizontalMargin * 2);
		}
		ParentRectTransform.sizeDelta = vector;
	}
}
