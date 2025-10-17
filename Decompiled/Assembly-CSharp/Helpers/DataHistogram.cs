using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Helpers;

public class DataHistogram
{
	private List<float> points = new List<float>();

	private int numberOfBins = 10;

	public void AddPoint(float value)
	{
		points.Add(value);
	}

	public void PrintHistogram()
	{
		if (points.Count == 0)
		{
			Debug.Log("Histogram: No data to show.");
			return;
		}
		float num = float.MaxValue;
		float num2 = float.MinValue;
		foreach (float point in points)
		{
			if (point < num)
			{
				num = point;
			}
			if (point > num2)
			{
				num2 = point;
			}
		}
		float num3 = (num2 - num) / (float)numberOfBins;
		int[] array = new int[numberOfBins];
		foreach (float point2 in points)
		{
			int num4 = (int)((point2 - num) / num3);
			if (num4 == numberOfBins)
			{
				num4--;
			}
			array[num4]++;
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < numberOfBins; i++)
		{
			stringBuilder.Append($"{num + (float)i * num3:0.##} - {num + (float)(i + 1) * num3:0.##} : ");
			int count = Mathf.Min(50, array[i]);
			stringBuilder.Append(new string('*', count));
			stringBuilder.AppendLine($" {array[i]}");
		}
		Debug.Log($"Histogram:\n{stringBuilder}");
	}

	public void Clear()
	{
		points.Clear();
	}
}
