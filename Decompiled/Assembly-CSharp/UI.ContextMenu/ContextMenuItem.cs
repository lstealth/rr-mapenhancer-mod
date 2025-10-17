using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.ContextMenu;

public class ContextMenuItem : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerClickHandler
{
	public Image image;

	public TMP_Text label;

	public CanvasGroup canvasGroup;

	public WedgeImage wedgeImage;

	public RectTransform textContainer;

	public Color colorDefault;

	public Color colorHover;

	public Action OnClick;

	private float _defaultFontSize;

	private void Awake()
	{
		_defaultFontSize = label.fontSize;
	}

	private void OnEnable()
	{
		LeanTween.color(wedgeImage.rectTransform, colorDefault, 0f);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		LeanTween.scale(image.rectTransform, 1.1f * Vector3.one, 0.15f).setEaseOutQuad();
		label.fontSize(_defaultFontSize * 1.1f, 0.15f).setEaseOutQuad();
		LeanTween.color(wedgeImage.rectTransform, colorHover, 0.25f).setEaseOutQuad();
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		LeanTween.scale(image.rectTransform, 1f * Vector3.one, 0.15f).setEaseInQuad();
		label.fontSize(_defaultFontSize, 0.15f).setEaseInQuad();
		LeanTween.color(wedgeImage.rectTransform, colorDefault, 0.35f).setEaseOutQuad();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		LeanTween.sequence().append(LeanTween.scale(image.rectTransform, 1.3f * Vector3.one, 0.05f).setEaseInQuad()).append(delegate
		{
			OnClick?.Invoke();
		})
			.append(LeanTween.scale(image.rectTransform, 1f * Vector3.one, 0.25f).setEaseOutQuad());
	}

	public void SetAngle(float startAngle, float angleRange)
	{
		wedgeImage.startAngle = startAngle;
		wedgeImage.angleRange = angleRange;
	}
}
