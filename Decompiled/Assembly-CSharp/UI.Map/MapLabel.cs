using TMPro;
using UnityEngine;

namespace UI.Map;

public class MapLabel : MonoBehaviour
{
	public enum Alignment
	{
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight
	}

	private Canvas _canvas;

	public string text;

	public Alignment alignment;

	public bool alignYUp;

	private Alignment? _oldAlignment;

	private void Awake()
	{
		_canvas = GetComponent<Canvas>();
	}

	private void OnEnable()
	{
		if (Application.isPlaying && alignYUp)
		{
			TMP_Text componentInChildren = GetComponentInChildren<TMP_Text>();
			if (componentInChildren != null)
			{
				componentInChildren.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
			}
		}
	}

	public void SetZoom(float s)
	{
		if (!(_canvas == null))
		{
			_canvas.transform.localScale = Vector3.one * s;
		}
	}

	private void OnValidate()
	{
		TMP_Text componentInChildren = GetComponentInChildren<TMP_Text>();
		if (!(componentInChildren == null))
		{
			componentInChildren.text = text;
		}
	}
}
