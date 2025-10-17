namespace Game;

public readonly struct PlayerId
{
	private readonly string _playerId;

	public static readonly PlayerId Invalid = new PlayerId(null);

	public string String => _playerId;

	public bool IsValid => !string.IsNullOrEmpty(_playerId);

	public PlayerId(string playerId)
	{
		_playerId = playerId;
	}

	public PlayerId(ulong steamId)
	{
		_playerId = steamId.ToString("D");
	}

	public override string ToString()
	{
		return _playerId;
	}

	public static bool operator ==(PlayerId a, PlayerId b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(PlayerId x, PlayerId y)
	{
		return !x.Equals(y);
	}

	public bool Equals(PlayerId other)
	{
		return _playerId == other._playerId;
	}

	public override bool Equals(object obj)
	{
		if (obj is PlayerId other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return _playerId.GetHashCode();
	}
}
