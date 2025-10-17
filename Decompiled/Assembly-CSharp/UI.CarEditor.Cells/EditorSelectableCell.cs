using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.CarEditor.Cells;

public class EditorSelectableCell : MonoBehaviour
{
	[SerializeField]
	public TMP_Text label;

	[SerializeField]
	public Button button;

	[SerializeField]
	public Image selectedImage;

	public bool IsSelected
	{
		get
		{
			return selectedImage.enabled;
		}
		set
		{
			selectedImage.enabled = value;
		}
	}

	public void Configure(string text, bool componentEnabled, Action onClick)
	{
		label.text = (componentEnabled ? text : ("<s>" + text + "</s>"));
		label.textStyle = TMP_Style.NormalStyle;
		button.onClick.AddListener(delegate
		{
			onClick();
		});
	}
}
