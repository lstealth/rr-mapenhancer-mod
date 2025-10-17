using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI;

public class DropdownLocationPickerRow : MonoBehaviour
{
	[SerializeField]
	private Button button;

	[SerializeField]
	private Image selectedImage;

	[SerializeField]
	private TMP_Text titleLabel;

	[SerializeField]
	private TMP_Text subtitleLabel;

	private Action _onClick;

	public void Configure(DropdownLocationPickerRowData data, bool selected, Action action)
	{
		titleLabel.text = data.Title;
		subtitleLabel.text = data.Subtitle;
		selectedImage.gameObject.SetActive(selected);
		_onClick = action;
	}

	public void ButtonClicked()
	{
		_onClick?.Invoke();
	}
}
