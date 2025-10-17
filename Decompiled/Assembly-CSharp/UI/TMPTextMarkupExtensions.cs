using Markroader;
using TMPro;

namespace UI;

public static class TMPTextMarkupExtensions
{
	public static void SetTextMarkup(this TMP_Text text, string markup)
	{
		text.text = markup.ToTMPMarkup();
	}
}
