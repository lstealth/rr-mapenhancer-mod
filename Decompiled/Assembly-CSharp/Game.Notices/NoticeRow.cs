using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Notices;

[RequireComponent(typeof(RectTransform))]
public class NoticeRow : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public TMP_Text label;

	public CanvasGroup dismissButtonGroup;

	public RectTransform contentRectTransform;

	public Action OnDismiss;

	private void Awake()
	{
		dismissButtonGroup.alpha = 0f;
	}

	public void ActionDismiss()
	{
		OnDismiss?.Invoke();
	}

	public void AnimatedDestroy()
	{
		LTSeq lTSeq = LeanTween.sequence();
		lTSeq.append(LeanTween.move(contentRectTransform, new Vector2(300f, 0f), 0.25f).setEaseInCubic());
		lTSeq.append(0.125f);
		lTSeq.append(delegate
		{
			UnityEngine.Object.Destroy(base.gameObject);
		});
	}

	public void SetOffscreen(bool offscreen, bool animated)
	{
		Vector2 vector = new Vector2(offscreen ? 300 : 0, 0f);
		if (animated)
		{
			LTDescr lTDescr = LeanTween.move(contentRectTransform, vector, 0.25f);
			if (offscreen)
			{
				lTDescr.setEaseOutCubic();
			}
			else
			{
				lTDescr.setEaseInCubic();
			}
		}
		else
		{
			contentRectTransform.anchoredPosition = vector;
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		LeanTween.alphaCanvas(dismissButtonGroup, 1f, 0.25f);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		LeanTween.alphaCanvas(dismissButtonGroup, 0f, 0.25f);
	}
}
