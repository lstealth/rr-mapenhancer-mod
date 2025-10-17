using System;
using System.Collections.Generic;
using UnityEngine;

namespace Helpers;

public static class ListExtension
{
	public static T Random<T>(this IReadOnlyList<T> list)
	{
		if (list.Count == 0)
		{
			throw new ArgumentException("List contains no elements", "list");
		}
		int index = UnityEngine.Random.Range(0, list.Count);
		return list[index];
	}

	public static void Shuffle<T>(this IList<T> list)
	{
		int num = list.Count;
		while (num > 1)
		{
			num--;
			int num2 = UnityEngine.Random.Range(0, num + 1);
			int index = num2;
			int index2 = num;
			T val = list[num];
			T val2 = list[num2];
			T val3 = (list[index] = val);
			val3 = (list[index2] = val2);
		}
	}

	public static void OverhandShuffle<T>(this List<T> list, System.Random rnd, int lumpSizeMin, int lumpSizeMax)
	{
		if (lumpSizeMin <= list.Count)
		{
			lumpSizeMin = Mathf.Max(1, lumpSizeMin);
			lumpSizeMax = Mathf.Min(lumpSizeMax, list.Count);
			if (lumpSizeMin > lumpSizeMax)
			{
				throw new ArgumentException();
			}
			_ = list.Count;
			List<T> list2 = new List<T>(list);
			list.Clear();
			while (list2.Count > 0)
			{
				int count = Mathf.Min(rnd.Next(lumpSizeMin, lumpSizeMax), list2.Count);
				list.InsertRange(0, list2.GetRange(0, count));
				list2.RemoveRange(0, count);
			}
		}
	}

	public static T RandomElementUsingNormalDistribution<T>(this IReadOnlyList<T> items, float normalizedCenter, System.Random random, float standardDeviationRange = 5f)
	{
		if (items == null || items.Count == 0)
		{
			throw new ArgumentException("The items array must not be null or empty.");
		}
		if (normalizedCenter < 0f || normalizedCenter > 1f)
		{
			throw new ArgumentException("Normalized center must be between 0 and 1.");
		}
		int count = items.Count;
		double num = normalizedCenter * (float)count;
		float num2 = (float)count / standardDeviationRange;
		int num5;
		do
		{
			double d = 1.0 - random.NextDouble();
			double num3 = 1.0 - random.NextDouble();
			double num4 = Math.Sqrt(-2.0 * Math.Log(d)) * Math.Sin(Math.PI * 2.0 * num3);
			num5 = (int)Math.Floor(num + (double)num2 * num4);
		}
		while (num5 < 0 || num5 >= count);
		return items[num5];
	}
}
