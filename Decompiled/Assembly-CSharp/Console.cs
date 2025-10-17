using UI.Console;

public static class Console
{
	public static void Log(string message)
	{
		UI.Console.Console shared = UI.Console.Console.shared;
		if (shared != null)
		{
			shared.AddLine(message);
		}
	}

	public static string ConsoleEscape(this string str)
	{
		return "<noparse>" + str + "</noparse>";
	}
}
