using System.Text;
using UnityEngine;

namespace Helpers;

public static class ColorHelper
{
	public static Color? ColorFromHex(string arg)
	{
		if (string.IsNullOrEmpty(arg))
		{
			return null;
		}
		if (arg.StartsWith("#") && ColorUtility.TryParseHtmlString(arg, out var color))
		{
			return color;
		}
		if (ColorUtility.TryParseHtmlString("#" + arg, out color))
		{
			return color;
		}
		Debug.LogWarning("Failed to parse color: \"" + arg + "\"");
		return null;
	}

	public static Color ColorFromHexLiteral(string arg)
	{
		return ColorFromHex(arg) ?? Color.white;
	}

	public static bool IsDark(this Color color)
	{
		float num = color.r * 256f;
		float num2 = color.g * 256f;
		float num3 = color.b * 256f;
		return Mathf.Sqrt(0.299f * (num * num) + 0.587f * (num2 * num2) + 0.114f * (num3 * num3)) < 127.5f;
	}

	public static string HexFromColor(Color color)
	{
		StringBuilder stringBuilder = new StringBuilder(8);
		stringBuilder.Append("#");
		stringBuilder.Append(Mathf.RoundToInt(color.r * 255f).ToString("X2"));
		stringBuilder.Append(Mathf.RoundToInt(color.g * 255f).ToString("X2"));
		stringBuilder.Append(Mathf.RoundToInt(color.b * 255f).ToString("X2"));
		return stringBuilder.ToString();
	}

	public static string HexString(this Color color)
	{
		return HexFromColor(color);
	}
}
