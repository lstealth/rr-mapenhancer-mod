using TMPro;
using UnityEngine;

namespace UI.Common;

public class ProgressIndicator : MonoBehaviour
{
	public CanvasGroup canvasGroup;

	public TextMeshProUGUI text;

	private static ProgressIndicator Instance { get; set; }

	public static void DrawCurtains()
	{
		if (Instance == null)
		{
			Debug.LogError("Couldn't find ProgressIndicator instance.");
		}
		else
		{
			Instance._DrawCurtains();
		}
	}

	public static void Show(string text, float delay)
	{
		if (Instance == null)
		{
			Debug.LogError("Couldn't find ProgressIndicator instance.");
		}
		else
		{
			Instance.Run(text, delay);
		}
	}

	public static void Hide()
	{
		Instance._Hide();
	}

	private void Awake()
	{
		Instance = this;
		canvasGroup.blocksRaycasts = false;
		canvasGroup.alpha = 0f;
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	private void _DrawCurtains()
	{
		text.text = "Loading...";
		LeanTween.cancel(canvasGroup.gameObject);
		canvasGroup.alpha = 1f;
	}

	private void Run(string toastText, float delay)
	{
		text.text = toastText;
		LeanTween.cancel(canvasGroup.gameObject);
		LTSeq lTSeq = LeanTween.sequence();
		lTSeq.append(delay);
		lTSeq.append(LeanTween.alphaCanvas(canvasGroup, 1f, 0.125f));
	}

	private void _Hide()
	{
		LeanTween.cancel(canvasGroup.gameObject);
		LeanTween.sequence().append(LeanTween.alphaCanvas(canvasGroup, 0f, 0.125f));
	}
}
