using System;
using UnityEngine;

namespace Helpers;

public static class StringExtensions
{
	public static string StripHtml(this string source)
	{
		char[] array = new char[source.Length];
		int num = 0;
		bool flag = false;
		foreach (char c in source)
		{
			switch (c)
			{
			case '<':
				flag = true;
				continue;
			case '>':
				flag = false;
				continue;
			}
			if (!flag)
			{
				array[num] = c;
				num++;
			}
		}
		return new string(array, 0, num);
	}

	public static string NoParse(this string value)
	{
		return "<noparse>" + value + "</noparse>";
	}

	public static string Truncate(this string value, int maxLength)
	{
		if (string.IsNullOrEmpty(value))
		{
			return value;
		}
		if (value.Length <= maxLength)
		{
			return value;
		}
		return value.Substring(0, Mathf.Min(value.Length, maxLength));
	}

	public static string RemovingLeadingWhitespaceFromLines(this string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return input;
		}
		string[] array = input.Split(new string[3] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
		string text = null;
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (!string.IsNullOrWhiteSpace(text2))
			{
				int j;
				for (j = 0; j < text2.Length && char.IsWhiteSpace(text2[j]); j++)
				{
				}
				text = text2.Substring(0, j);
				break;
			}
		}
		if (text == null || text.Length == 0)
		{
			return input;
		}
		for (int k = 0; k < array.Length; k++)
		{
			if (array[k].StartsWith(text))
			{
				array[k] = array[k].Substring(text.Length);
			}
		}
		return string.Join(Environment.NewLine, array);
	}
}
