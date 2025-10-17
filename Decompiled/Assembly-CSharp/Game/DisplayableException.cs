using System;

namespace Game;

public class DisplayableException : Exception
{
	public string DisplayMessage { get; private set; }

	public DisplayableException(string displayMessage, Exception inner = null)
		: base(displayMessage, inner)
	{
		DisplayMessage = displayMessage;
	}
}
