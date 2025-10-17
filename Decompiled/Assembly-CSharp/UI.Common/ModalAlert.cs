using System;
using UI.Builder;
using UnityEngine;

namespace UI.Common;

public class ModalAlert : MonoBehaviour
{
	[SerializeField]
	private CanvasGroup canvasGroup;

	[SerializeField]
	private RectTransform alertRectTransform;

	[SerializeField]
	private RectTransform contentRectTransform;

	[SerializeField]
	private UIBuilderAssets builderAssets;

	private bool _actionSelected;

	private UIPanel _panel;

	private void Present()
	{
		canvasGroup.alpha = 0f;
		LTSeq lTSeq = LeanTween.sequence();
		lTSeq.append(LeanTween.scale(alertRectTransform, Vector3.one * 0.5f, 0f).setIgnoreTimeScale(useUnScaledTime: true));
		lTSeq.append(LeanTween.scale(alertRectTransform, Vector3.one * 1f, 0.25f).setEaseOutElastic().setIgnoreTimeScale(useUnScaledTime: true));
		LeanTween.sequence().append(LeanTween.alphaCanvas(canvasGroup, 1f, 0.25f).setIgnoreTimeScale(useUnScaledTime: true));
	}

	private void Dismiss()
	{
		LTSeq lTSeq = LeanTween.sequence();
		lTSeq.append(LeanTween.alphaCanvas(canvasGroup, 0f, 0.125f).setIgnoreTimeScale(useUnScaledTime: true));
		lTSeq.append(LeanTween.delayedCall(0.25f, (Action)delegate
		{
			_panel?.Dispose();
			_panel = null;
			if (base.gameObject != null)
			{
				UnityEngine.Object.Destroy(base.gameObject);
			}
		}).setIgnoreTimeScale(useUnScaledTime: true));
	}

	public void Configure(Action<UIPanelBuilder, Action> builderDismissClosure, int width)
	{
		Vector2 sizeDelta = alertRectTransform.sizeDelta;
		sizeDelta.x = width;
		alertRectTransform.sizeDelta = sizeDelta;
		_panel = UIPanel.Create(contentRectTransform, builderAssets, delegate(UIPanelBuilder builder)
		{
			builderDismissClosure?.Invoke(builder, Dismiss);
		});
		Present();
	}
}
