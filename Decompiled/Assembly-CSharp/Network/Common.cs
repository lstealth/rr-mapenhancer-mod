namespace Network;

public static class Common
{
	public struct ProtocolVersion
	{
		public int Major;

		public int Minor;
	}

	public const int DefaultPort = 11722;

	public static ProtocolVersion CurrentVersion = new ProtocolVersion
	{
		Major = 0,
		Minor = 3
	};

	public static ProtocolVersion MinimumVersion = new ProtocolVersion
	{
		Major = 0,
		Minor = 3
	};
}
