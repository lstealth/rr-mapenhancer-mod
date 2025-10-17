using System.Linq;

namespace UI.CarEditor;

internal static class Extensions
{
	public static string ToSentence(this string input)
	{
		return new string(input.SelectMany((char c, int i) => (i > 0 && char.IsUpper(c)) ? new char[2] { ' ', c } : new char[1] { c }).ToArray());
	}
}
