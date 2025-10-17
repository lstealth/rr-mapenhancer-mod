using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Builder;

[RequireComponent(typeof(RectTransform))]
public class TableCell : MonoBehaviour
{
	private RectTransform _rectTransform;

	[Tooltip("Top, right, bottom, left.")]
	[SerializeField]
	private Image[] borderImages;

	[SerializeField]
	private Button backgroundButton;

	[SerializeField]
	private Image backgroundImage;

	public TMP_Text text;

	public TMP_Text trailingText;

	private Action _onClick;

	private void Awake()
	{
		_rectTransform = GetComponent<RectTransform>();
	}

	public void Configure(TableBuilderConfig config, string labelText, string labelTrailingText, byte borderMask, bool highlighted, Action onClick)
	{
		SetBorders(borderMask, config.BorderOpacity);
		text.text = labelText;
		text.textWrappingMode = config.TextWrappingMode;
		text.overflowMode = config.TextOverflowMode;
		text.margin = config.TextMargin;
		bool flag = !string.IsNullOrEmpty(labelTrailingText);
		trailingText.gameObject.SetActive(flag);
		if (flag)
		{
			trailingText.text = labelTrailingText;
			float z = config.TextMargin.z;
			trailingText.rectTransform.anchoredPosition = new Vector2(0f - (z - 1f), 0f);
			trailingText.rectTransform.sizeDelta = new Vector2(z, 0f);
		}
		_onClick = onClick;
		backgroundButton.gameObject.SetActive(_onClick != null);
		backgroundImage.enabled = highlighted;
	}

	public void SetBorders(byte borderMask, float opacity)
	{
		borderImages[0].enabled = (borderMask & 1) != 0;
		borderImages[1].enabled = (borderMask & 2) != 0;
		borderImages[2].enabled = (borderMask & 4) != 0;
		borderImages[3].enabled = (borderMask & 8) != 0;
		for (int i = 0; i < 4; i++)
		{
			borderImages[i].color *= opacity;
		}
	}

	public void HandleClick()
	{
		_onClick?.Invoke();
	}

	public void ConfigureAutoSize(int min, int max)
	{
		text.fontSizeMin = min;
		text.fontSizeMax = max;
	}
}
