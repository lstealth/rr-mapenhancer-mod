using TMPro;
using UnityEngine;

namespace UI;

public class MarkupTextBox : MonoBehaviour
{
	[TextArea]
	[SerializeField]
	private string content;

	[SerializeField]
	private TMP_Text text;

	private void OnEnable()
	{
		Populate(content);
	}

	private void OnValidate()
	{
		Populate(content);
	}

	private void Populate(string str)
	{
		text.SetTextMarkup(str);
	}
}
