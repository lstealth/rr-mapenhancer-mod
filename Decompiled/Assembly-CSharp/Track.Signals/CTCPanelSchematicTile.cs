using UnityEngine;

namespace Track.Signals;

[RequireComponent(typeof(RectTransform))]
public class CTCPanelSchematicTile : MonoBehaviour
{
	[Range(0f, 2f)]
	public int row;

	public RectTransform RectTransform => GetComponent<RectTransform>();

	public float Width => RectTransform.rect.width;
}
