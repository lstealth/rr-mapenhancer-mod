using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI;

public class Callout : MonoBehaviour
{
	[SerializeField]
	private TMP_Text titleLabel;

	[SerializeField]
	private TMP_Text textLabel;

	[Tooltip("Optional. If set, is intended to be rotated externally to indicate off-screen direction.")]
	public Image directionalImage;

	private TooltipInfo _tooltipInfo;

	private LayoutElement _textLayoutElement;

	public string Title
	{
		get
		{
			return titleLabel.text;
		}
		set
		{
			titleLabel.text = value;
		}
	}

	public string Text
	{
		get
		{
			return textLabel.text;
		}
		set
		{
			textLabel.text = value;
		}
	}

	public RectTransform RectTransform { get; private set; }

	public bool IsEmpty => _tooltipInfo.IsEmpty;

	private void Awake()
	{
		RectTransform = GetComponent<RectTransform>();
		_textLayoutElement = textLabel.GetComponent<LayoutElement>();
	}

	public void Layout(float wrapWidth = 300f)
	{
		_textLayoutElement.preferredWidth = ((textLabel.preferredWidth > wrapWidth) ? wrapWidth : (-1f));
		LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);
	}

	public void SetTooltipInfo(TooltipInfo tooltipInfo)
	{
		if (!_tooltipInfo.Equals(tooltipInfo))
		{
			_tooltipInfo = tooltipInfo;
			string text = _tooltipInfo.Text;
			if (text != null && text.StartsWith("Click to"))
			{
				text = text.Replace("Click to", "<sprite name=\"MouseLeft\">");
			}
			Title = _tooltipInfo.Title;
			Text = text;
			Layout();
		}
	}
}
