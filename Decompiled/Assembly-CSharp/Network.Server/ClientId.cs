namespace Network.Server;

public readonly struct ClientId
{
	private readonly ulong _clientId;

	public ClientId(ulong clientId)
	{
		_clientId = clientId;
	}

	public override string ToString()
	{
		ulong clientId = _clientId;
		return clientId.ToString();
	}

	public bool Equals(ClientId other)
	{
		return _clientId == other._clientId;
	}

	public override bool Equals(object obj)
	{
		if (obj is ClientId other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		ulong clientId = _clientId;
		return clientId.GetHashCode();
	}
}
