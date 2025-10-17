using System;
using System.Security.Cryptography;
using Serilog;

namespace Game.State;

public static class StateManagerPasswordExtensions
{
	private const int SaltLength = 16;

	private const int HashLength = 20;

	private const int Iterations = 10000;

	public static void SetNewPlayerPassword(this GameStorage storage, string password)
	{
		if (string.IsNullOrEmpty(password))
		{
			storage.NewPlayerPasswordHash = "";
			return;
		}
		string propertyValue = (storage.NewPlayerPasswordHash = HashPassword(password));
		Log.Information("Password hashed to: {hash}", propertyValue);
	}

	public static bool CheckNewPlayerPassword(this GameStorage storage, string password)
	{
		if (!storage.HasNewPlayerPassword)
		{
			return true;
		}
		string newPlayerPasswordHash = storage.NewPlayerPasswordHash;
		return CheckPasswordAgainstHash(password, newPlayerPasswordHash);
	}

	private static bool CheckPasswordAgainstHash(string password, string savedPasswordHash)
	{
		if (string.IsNullOrEmpty(savedPasswordHash))
		{
			return true;
		}
		if (string.IsNullOrEmpty(password))
		{
			return false;
		}
		byte[] array = Convert.FromBase64String(savedPasswordHash);
		if (array.Length != 36)
		{
			return false;
		}
		byte[] array2 = new byte[16];
		Array.Copy(array, 0, array2, 0, 16);
		Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, array2, 10000);
		byte[] bytes = rfc2898DeriveBytes.GetBytes(20);
		rfc2898DeriveBytes.Dispose();
		ReadOnlySpan<byte> left = bytes;
		Span<byte> span = array.AsSpan();
		return CryptographicOperations.FixedTimeEquals(left, span.Slice(16, span.Length - 16));
	}

	private static string HashPassword(string password)
	{
		byte[] array = new byte[16];
		RandomNumberGenerator.Fill(array);
		Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, array, 10000);
		byte[] bytes = rfc2898DeriveBytes.GetBytes(20);
		rfc2898DeriveBytes.Dispose();
		byte[] array2 = new byte[36];
		Array.Copy(array, 0, array2, 0, 16);
		Array.Copy(bytes, 0, array2, 16, 20);
		return Convert.ToBase64String(array2);
	}
}
