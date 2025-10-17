using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI;

public class DropdownMenuItem : MonoBehaviour
{
	[SerializeField]
	private Button button;

	[SerializeField]
	private Image checkImage;

	[SerializeField]
	private TMP_Text titleLabel;

	[SerializeField]
	private TMP_Text subtitleLabel;

	private Action _onClick;

	public void Configure(DropdownMenu.RowData data, Action action)
	{
		checkImage.gameObject.SetActive(data.Check == DropdownMenu.CheckState.Checked);
		titleLabel.text = data.Title;
		subtitleLabel.text = data.Subtitle;
		_onClick = action;
	}

	public float GetPreferredWidth()
	{
		return Mathf.Max(titleLabel.GetPreferredValues().x, subtitleLabel.GetPreferredValues().x) + 28f;
	}

	public void ButtonClicked()
	{
		_onClick?.Invoke();
	}
}
