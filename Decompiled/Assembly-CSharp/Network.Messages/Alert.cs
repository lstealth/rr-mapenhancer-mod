using MessagePack;

namespace Network.Messages;

[MessagePackObject(false)]
public struct Alert : INetworkMessage
{
	[Key(0)]
	public AlertStyle Style;

	[Key(1)]
	public AlertLevel Level;

	[Key(2)]
	public string Message;

	[Key(3)]
	public double Timestamp;

	public Alert(AlertStyle style, AlertLevel level, string message, double timestamp)
	{
		Style = style;
		Level = level;
		Message = message;
		Timestamp = timestamp;
	}
}
