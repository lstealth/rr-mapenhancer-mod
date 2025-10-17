using Game;
using Serilog;
using UnityEngine;

namespace Analytics;

public class EarlyAccessSplash : MonoBehaviour
{
	public CanvasGroup backgroundGroup;

	public CanvasGroup foregroundGroup;

	private void Awake()
	{
		backgroundGroup.alpha = 0f;
		foregroundGroup.alpha = 0f;
		SetBlocksRaycasts(blocks: false);
	}

	private void OnEnable()
	{
		if (Preferences.Analytics == Preferences.AnalyticsPref.Unknown)
		{
			Show();
		}
	}

	public void Show()
	{
		SetBlocksRaycasts(blocks: true);
		LTSeq lTSeq = LeanTween.sequence();
		lTSeq.append(LeanTween.alphaCanvas(backgroundGroup, 1f, 0.25f).setIgnoreTimeScale(useUnScaledTime: true));
		lTSeq.append(LeanTween.alphaCanvas(foregroundGroup, 1f, 0.125f).setIgnoreTimeScale(useUnScaledTime: true));
	}

	private void Dismiss()
	{
		LTSeq lTSeq = LeanTween.sequence();
		lTSeq.append(LeanTween.alphaCanvas(foregroundGroup, 0f, 0.25f).setIgnoreTimeScale(useUnScaledTime: true));
		lTSeq.append(LeanTween.alphaCanvas(backgroundGroup, 0f, 0.5f).setIgnoreTimeScale(useUnScaledTime: true));
		lTSeq.append(delegate
		{
			SetBlocksRaycasts(blocks: false);
		});
	}

	private void SetBlocksRaycasts(bool blocks)
	{
		backgroundGroup.blocksRaycasts = blocks;
		foregroundGroup.blocksRaycasts = blocks;
	}

	public void ButtonOptIn()
	{
		Log.Information("EA: Analytics Opt In");
		Preferences.Analytics = Preferences.AnalyticsPref.OptIn;
		Dismiss();
	}

	public void ButtonOptOut()
	{
		Log.Information("EA: Analytics Opt Out");
		Preferences.Analytics = Preferences.AnalyticsPref.OptOut;
		Dismiss();
	}
}
