namespace Network;

public enum DisconnectReason
{
	Invalid = 0,
	Goodbye = 1001,
	NoMorePassengers = 1002,
	AccessDenied = 2001,
	VersionMismatch = 2002,
	PasswordRequired = 2003,
	PeerSentNoConnection = 5010,
	Timeout = 5003,
	HostClosedConnection = 2999
}
