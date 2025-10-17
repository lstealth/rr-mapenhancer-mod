using TMPro;

namespace UI.Console;

public class ConsoleLine
{
	public TMP_Text Label;

	public readonly string Text;

	public readonly float Timestamp;

	public ConsoleLine(TMP_Text label, string text, float timestamp)
	{
		Label = label;
		Text = text;
		Timestamp = timestamp;
	}
}
