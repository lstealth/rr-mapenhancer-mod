using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI;

public static class ScrollRectExtensions
{
	private static readonly Vector3[] StaticExtentsStorage = new Vector3[4];

	public static void ScrollToVisible(this ScrollRect scrollRect, RectTransform target, ScrollPosition scrollPosition)
	{
		Canvas.ForceUpdateCanvases();
		RectTransform rectTransform = (RectTransform)scrollRect.transform;
		Vector2 vector = rectTransform.InverseTransformPoint(scrollRect.content.position);
		target.GetExtentsLocal(rectTransform, out var topLeft, out var bottomRight);
		Vector2 vector2 = bottomRight - topLeft;
		Vector2 anchoredPosition = scrollRect.content.anchoredPosition;
		if (scrollRect.horizontal)
		{
			anchoredPosition.x = vector.x - topLeft.x + vector2.x / 2f;
			anchoredPosition.x = Mathf.Clamp(anchoredPosition.x, 0f, scrollRect.content.rect.width - scrollRect.viewport.rect.width);
		}
		if (scrollRect.vertical)
		{
			float height = scrollRect.viewport.rect.height;
			switch (scrollPosition)
			{
			case ScrollPosition.Top:
				anchoredPosition.y = vector.y - topLeft.y + vector2.y;
				break;
			case ScrollPosition.OneThird:
				anchoredPosition.y = vector.y - topLeft.y + vector2.y - height * 0.33f;
				break;
			default:
				throw new ArgumentOutOfRangeException("scrollPosition", scrollPosition, null);
			}
			anchoredPosition.y = Mathf.Clamp(anchoredPosition.y, 0f, scrollRect.content.rect.height - height);
		}
		scrollRect.content.anchoredPosition = anchoredPosition;
	}

	public static void GetExtentsLocal(this RectTransform rectTransform, RectTransform localTo, out Vector2 topLeft, out Vector2 bottomRight)
	{
		Vector3[] staticExtentsStorage = StaticExtentsStorage;
		rectTransform.GetWorldCorners(staticExtentsStorage);
		topLeft = localTo.InverseTransformPoint(staticExtentsStorage[1]);
		bottomRight = localTo.InverseTransformPoint(staticExtentsStorage[3]);
	}

	public static IEnumerator ScrollAnimated(this ScrollRect scrollRect, float targetPosition, float duration)
	{
		float initialPosition = scrollRect.verticalNormalizedPosition;
		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			float t = elapsed / duration;
			scrollRect.verticalNormalizedPosition = Mathf.SmoothStep(initialPosition, targetPosition, t);
			yield return null;
		}
		scrollRect.verticalNormalizedPosition = targetPosition;
	}
}
