using System.Text.RegularExpressions;

namespace Helpers;

public static class StringSanitizer
{
	public static string SanitizeName(string str)
	{
		if (str == null)
		{
			return null;
		}
		str = Regex.Replace(str, "[<>\\[\\]]", string.Empty);
		str = str.Trim();
		return str;
	}
}
