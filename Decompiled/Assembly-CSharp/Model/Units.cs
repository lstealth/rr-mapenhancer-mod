using UnityEngine;

namespace Model;

public static class Units
{
	public const float MpsToMph = 2.23694f;

	public const float MphToMps = 0.44703928f;

	public const float MetersToMiles = 0.0006213712f;

	public const float MilesToMeters = 1609.344f;

	public const float KmToMiles = 0.6213712f;

	public const float MilesToKm = 1.609344f;

	public const float MetersToFeet = 3.28084f;

	public const float FeetToMeters = 0.3048f;

	public const float MetersToInches = 39.37008f;

	public const float LbToKg = 0.4536f;

	public const float KgToLb = 2.2045856f;

	public const float LbToNewton = 4.44822f;

	public const float NewtonToLb = 0.22480904f;

	public const float TonToLb = 2000f;

	public const float LbToTon = 0.0005f;

	public static string DistanceText(float distanceInMeters)
	{
		float num = distanceInMeters * 0.0006213712f;
		float num2 = distanceInMeters * 3.28084f;
		if (num2 < 500f)
		{
			return $"{Mathf.RoundToInt(num2)} ft";
		}
		if (num >= 3f)
		{
			return $"{Mathf.RoundToInt(num)} mi";
		}
		return $"{num:N1} mi";
	}
}
