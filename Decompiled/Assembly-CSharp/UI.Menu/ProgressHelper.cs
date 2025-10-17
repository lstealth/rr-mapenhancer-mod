using UnityEngine;

namespace UI.Menu;

internal class ProgressHelper
{
	private PersistentLoader _persistentLoader;

	private float _start;

	private float _portion = 1f;

	private string progressText = "";

	public ProgressHelper(PersistentLoader persistentLoader)
	{
		_persistentLoader = persistentLoader;
	}

	public void SetBounds(float start, float portion)
	{
		_start = start;
		_portion = portion;
	}

	public void ShowProgress(float progress)
	{
		float num = _start + progress * _portion;
		_persistentLoader.ShowProgress(Mathf.RoundToInt(100f * num), progressText);
	}

	public void ShowProgressText(float progress, string text)
	{
		progressText = text;
		ShowProgress(progress);
	}
}
