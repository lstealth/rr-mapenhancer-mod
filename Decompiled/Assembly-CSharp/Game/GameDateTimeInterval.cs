using System.Text;
using Core;
using UnityEngine;

namespace Game;

public static class GameDateTimeInterval
{
	public enum Style
	{
		Full,
		Short
	}

	public static string IntervalString(this GameDateTime self, GameDateTime relativeTo, Style style = Style.Full)
	{
		return DeltaStringMinutes(Mathf.FloorToInt((float)(relativeTo.TotalSeconds - self.TotalSeconds) / 60f), style);
	}

	public static string DeltaStringMinutes(int totalMinutes, Style style = Style.Full)
	{
		totalMinutes = Mathf.Abs(totalMinutes);
		int num = totalMinutes / 1440;
		int num2 = (totalMinutes - num * 24 * 60) / 60;
		int num3 = totalMinutes - (num * 24 * 60 + num2 * 60);
		StringBuilder stringBuilder = new StringBuilder();
		switch (style)
		{
		case Style.Full:
			if (num > 0)
			{
				stringBuilder.Append(num.Pluralize("day") + " ");
			}
			if (num2 > 0)
			{
				stringBuilder.Append(num2.Pluralize("hour") + " ");
			}
			if ((num3 > 0 && num <= 0) || totalMinutes == 0)
			{
				stringBuilder.Append(num3.Pluralize("minute") + " ");
			}
			break;
		case Style.Short:
			if (num > 0)
			{
				stringBuilder.Append($"{num}:{num2:D2}:{num3:D2}");
			}
			else
			{
				stringBuilder.Append($"{num2}:{num3:D2}");
			}
			break;
		}
		return stringBuilder.ToString().Trim();
	}
}
