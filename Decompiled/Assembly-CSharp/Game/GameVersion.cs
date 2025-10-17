using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game;

public readonly struct GameVersion
{
	private static GameVersion? _cachedCurrent;

	public static readonly GameVersion V2024_4_0 = new GameVersion("2024.4.0");

	public static readonly GameVersion Zero = new GameVersion("", 0);

	public readonly string Version;

	private readonly int _intVersion;

	private static Regex _versionRegex;

	public static GameVersion Current
	{
		get
		{
			GameVersion valueOrDefault = _cachedCurrent.GetValueOrDefault();
			if (!_cachedCurrent.HasValue)
			{
				valueOrDefault = new GameVersion(Application.version);
				_cachedCurrent = valueOrDefault;
			}
			return _cachedCurrent.Value;
		}
	}

	public bool IsZero => _intVersion == 0;

	public GameVersion(string version)
	{
		Version = version;
		if (!TryParse(version, out _intVersion))
		{
			throw new Exception("Failed to parse version: " + version);
		}
	}

	private GameVersion(string version, int intVersion)
	{
		Version = version;
		_intVersion = intVersion;
	}

	public override string ToString()
	{
		return Version;
	}

	public static GameVersion FromStringOrZero(string versionString)
	{
		try
		{
			return new GameVersion(versionString);
		}
		catch
		{
			return Zero;
		}
	}

	private static bool TryParse(string versionString, out int intVersion)
	{
		if (_versionRegex == null)
		{
			_versionRegex = new Regex("(\\d+)\\.(\\d+)\\.(\\d+)([a-z]?)");
		}
		Match match = _versionRegex.Match(versionString);
		if (!match.Success)
		{
			intVersion = 0;
			return false;
		}
		int num = int.Parse(match.Groups[1].Value);
		int num2 = int.Parse(match.Groups[2].Value);
		int num3 = int.Parse(match.Groups[3].Value);
		int num4 = 0;
		if (!string.IsNullOrEmpty(match.Groups[4].Value))
		{
			char c = match.Groups[4].Value[0];
			num4 = (char.IsLetter(c) ? (c - 97 + 1) : 0);
		}
		intVersion = num * 1000000 + num2 * 10000 + num3 * 100 + num4;
		return true;
	}

	public static bool operator <(GameVersion a, GameVersion b)
	{
		return a._intVersion < b._intVersion;
	}

	public static bool operator >(GameVersion a, GameVersion b)
	{
		return a._intVersion > b._intVersion;
	}

	public static bool operator <=(GameVersion a, GameVersion b)
	{
		return a._intVersion <= b._intVersion;
	}

	public static bool operator >=(GameVersion a, GameVersion b)
	{
		return a._intVersion >= b._intVersion;
	}

	public static bool operator ==(GameVersion a, GameVersion b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(GameVersion a, GameVersion b)
	{
		return !a.Equals(b);
	}

	public bool Equals(GameVersion other)
	{
		return _intVersion == other._intVersion;
	}

	public override bool Equals(object obj)
	{
		if (obj is GameVersion other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return _intVersion;
	}
}
