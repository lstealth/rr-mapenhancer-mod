using TMPro;
using UnityEngine.UI;

namespace Helpers;

public static class ButtonExtensions
{
	public static TMP_Text TMPText(this Button button)
	{
		return button.GetComponentInChildren<TMP_Text>();
	}
}
