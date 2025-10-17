using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI;

public class ListRow : MonoBehaviour
{
	[SerializeField]
	public TMP_Text text;

	[SerializeField]
	public Button button;

	[SerializeField]
	private Image selectedImage;

	[SerializeField]
	private TMP_FontAsset selectedFont;

	private bool _hasConfiguredDefaults;

	private bool _selected;

	private Color _defaultTextColor;

	private TMP_FontAsset _defaultTextFont;

	private void Awake()
	{
		if (!_hasConfiguredDefaults)
		{
			_hasConfiguredDefaults = true;
			_defaultTextColor = text.color;
			_defaultTextFont = text.font;
			if (selectedImage != null)
			{
				selectedImage.color = _defaultTextColor;
				selectedImage.enabled = false;
			}
		}
	}

	private void OnEnable()
	{
		UpdateForSelected();
	}

	public void SetSelected(bool selected)
	{
		_selected = selected;
		UpdateForSelected();
	}

	private void UpdateForSelected()
	{
		if (_hasConfiguredDefaults)
		{
			bool selected = _selected;
			if (selectedImage != null)
			{
				selectedImage.enabled = selected;
			}
			text.font = (selected ? selectedFont : _defaultTextFont);
			text.color = (selected ? Color.black : _defaultTextColor);
		}
	}
}
