using UnityEngine;

public static class TextSprites
{
	public const string MouseLeft = "<sprite name=\"MouseLeft\">";

	public const string MouseNo = "<sprite name=\"MouseNo\">";

	public const string Plus = "<sprite name=\"Plus\">";

	public const string Minus = "<sprite name=\"Minus\">";

	public const string NotAuthorized = "<sprite name=\"MouseNo\"> N/A";

	public const string Destination = "<sprite name=\"Destination\">";

	public const string Spotted = "<sprite name=\"Spotted\">";

	public const string HandbrakeWheel = "<sprite name=\"HandbrakeWheel\">";

	public const string Hotbox = "<sprite name=\"Flame\">";

	public const string Locked = "<sprite name=\"Locked\">";

	public const string Unlocked = "<sprite name=\"Unlocked\">";

	public const string Coupled = "<sprite name=Coupled>";

	public const string CopyToCoupled = "<sprite name=Copy><sprite name=Coupled>";

	public const string CycleWaybills = "<sprite name=CycleWaybills>";

	public const string Copy = "<sprite name=Copy>";

	public const string Warning = "<sprite name=Warning>";

	public static string PiePercent(float quantity, float capacity)
	{
		int num;
		if (capacity <= 0f)
		{
			num = 0;
		}
		else
		{
			float num2 = Mathf.Clamp01(quantity / capacity);
			int num3 = ((!(num2 < 0.01f)) ? ((!(num2 > 0.99f)) ? (Mathf.FloorToInt(num2 * 15f) + 1) : 16) : 0);
			num = num3;
		}
		return $"<sprite tint=1 color=#BBB29F name=Pie{num:D2}>";
	}
}
