public static class TextStyles
{
	private const string BoxcarRed = "b46457";

	private const string Orange = "A97A48";

	private const string Yellow = "DBB95C";

	private const string Green = "82a550";

	public static string Color(this string text, string color)
	{
		return "<color=#" + color + "ff>" + text + "</color>";
	}

	public static string ColorRed(this string text)
	{
		return text.Color("b46457");
	}

	public static string ColorOrange(this string text)
	{
		return text.Color("A97A48");
	}

	public static string ColorYellow(this string text)
	{
		return text.Color("DBB95C");
	}

	public static string ColorGreen(this string text)
	{
		return text.Color("82a550");
	}
}
