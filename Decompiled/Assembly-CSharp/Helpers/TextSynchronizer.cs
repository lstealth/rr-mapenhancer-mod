using TMPro;
using UnityEngine;

namespace Helpers;

public class TextSynchronizer : MonoBehaviour
{
	public string text;

	private void OnValidate()
	{
		ApplyText();
	}

	public void ApplyText()
	{
		TMP_Text[] componentsInChildren = base.transform.GetComponentsInChildren<TMP_Text>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].text = text;
		}
	}
}
