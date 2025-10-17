using System;
using TMPro;
using UnityEngine;

namespace UI.Map;

[RequireComponent(typeof(Canvas))]
public class MapIcon : MonoBehaviour, IMapClickable
{
	private Canvas _canvas;

	private TMP_Text _text;

	public Action OnClick { get; set; }

	private TMP_Text Text
	{
		get
		{
			if (_text == null)
			{
				_text = GetComponentInChildren<TMP_Text>();
			}
			return _text;
		}
	}

	public void Click()
	{
		OnClick?.Invoke();
	}

	private void Awake()
	{
		_canvas = GetComponent<Canvas>();
	}

	private void OnEnable()
	{
		MapBuilder shared = MapBuilder.Shared;
		if (shared != null)
		{
			shared.Add(this);
		}
	}

	private void OnDisable()
	{
		MapBuilder shared = MapBuilder.Shared;
		if (shared != null)
		{
			shared.Remove(this);
		}
	}

	public void SetZoom(float s)
	{
		_canvas.transform.localScale = Vector3.one * s;
		FixTextRotation();
	}

	public void SetText(string text)
	{
		TMP_Text text2 = Text;
		if (!(text2 == null))
		{
			text2.text = text;
		}
	}

	private void FixTextRotation()
	{
		TMP_Text text = Text;
		if (!(text == null))
		{
			float num = Mathf.DeltaAngle(text.transform.rotation.eulerAngles.y, -90f);
			if (-90f < num && num < 90f)
			{
				text.transform.localRotation = Quaternion.Euler(text.transform.localRotation.eulerAngles + new Vector3(0f, 0f, 180f));
			}
		}
	}
}
