using UnityEngine;

namespace Track.Signals.Panel;

public class CTCPanelSchematicFace : MonoBehaviour
{
	public Canvas canvas;

	public RectTransform RectTransform { get; set; }

	private void Awake()
	{
		RectTransform = canvas.GetComponent<RectTransform>();
	}
}
