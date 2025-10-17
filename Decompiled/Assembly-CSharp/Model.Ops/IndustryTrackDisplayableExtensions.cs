using System;
using System.Linq;
using Track;
using UnityEngine;

namespace Model.Ops;

public static class IndustryTrackDisplayableExtensions
{
	public static int TrackGrouping(this IIndustryTrackDisplayable displayable)
	{
		return displayable.TrackSpans.Aggregate(typeof(TrackSpan).GetHashCode(), HashCode.Combine);
	}

	public static string ShortName(this IIndustryTrackDisplayable ic, Industry industry)
	{
		string text = ic.DisplayName;
		string name = industry.name;
		if (text == name)
		{
			return text;
		}
		if (StartsWithSamePrefix(name, text, out var numberOfCharacters) && numberOfCharacters > 3)
		{
			string text2 = text;
			int num = numberOfCharacters;
			text = text2.Substring(num, text2.Length - num);
		}
		return text;
	}

	private static bool StartsWithSamePrefix(string a, string b, out int numberOfCharacters)
	{
		numberOfCharacters = 0;
		int num = Mathf.Min(a.Length, b.Length);
		int num2 = 0;
		for (int i = 0; i < num && a[i] == b[i]; i++)
		{
			num2++;
		}
		if (num2 <= 0)
		{
			return false;
		}
		string[] array = a.Split(" ");
		string[] array2 = b.Split(" ");
		for (int j = 0; j < Mathf.Min(array.Length, array2.Length) && !(array[j] != array2[j]); j++)
		{
			numberOfCharacters += 1 + array[j].Length;
		}
		return numberOfCharacters > 0;
	}
}
