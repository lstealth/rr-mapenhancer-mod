using System;
using TMPro;
using UnityEngine;

namespace UI.Common;

public class Toast : MonoBehaviour
{
	public RectTransform rectTransform;

	public CanvasGroup canvasGroup;

	public TextMeshProUGUI text;

	private RectTransform _parentRectTransform;

	private static Toast _instance;

	public static void Present(string text, ToastPosition position = ToastPosition.Middle)
	{
		if (_instance == null)
		{
			_instance = UnityEngine.Object.FindObjectOfType<Toast>();
		}
		if (_instance == null)
		{
			Debug.LogError("Couldn't find Toast instance in scene.");
		}
		else
		{
			_instance.Run(text, position);
		}
	}

	private void Awake()
	{
		canvasGroup.blocksRaycasts = false;
		canvasGroup.alpha = 0f;
		_parentRectTransform = rectTransform.parent.GetComponentInParent<RectTransform>();
	}

	private void Run(string toastText, ToastPosition position)
	{
		canvasGroup.alpha = 0f;
		text.text = toastText;
		float num = position switch
		{
			ToastPosition.Middle => 0.5f, 
			ToastPosition.Bottom => 0f, 
			_ => throw new ArgumentOutOfRangeException("position", position, null), 
		};
		float num2 = _parentRectTransform.rect.height - 100f;
		Vector2 anchoredPosition = rectTransform.anchoredPosition;
		anchoredPosition.y = num * num2 - num2 / 2f;
		rectTransform.anchoredPosition = anchoredPosition;
		LeanTween.cancel(rectTransform.gameObject);
		LeanTween.cancel(canvasGroup.gameObject);
		LTSeq lTSeq = LeanTween.sequence();
		lTSeq.append(LeanTween.scale(rectTransform, Vector3.one * 0.75f, 0f));
		lTSeq.append(LeanTween.scale(rectTransform, Vector3.one * 1f, 0.5f).setEaseOutElastic());
		lTSeq.append(1.9f);
		lTSeq.append(LeanTween.scale(rectTransform, Vector3.one * 0.5f, 0.5f).setEaseInCubic());
		LTSeq lTSeq2 = LeanTween.sequence();
		lTSeq2.append(LeanTween.alphaCanvas(canvasGroup, 1f, 0.25f));
		lTSeq2.append(2.25f);
		lTSeq2.append(LeanTween.alphaCanvas(canvasGroup, 0f, 0.25f));
	}
}
