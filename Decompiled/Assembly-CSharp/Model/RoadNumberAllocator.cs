using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Model;

public class RoadNumberAllocator
{
	private struct Record
	{
		public HashSet<string> Prefixes;
	}

	private readonly Dictionary<int, Record> _used = new Dictionary<int, Record>();

	private readonly Dictionary<int, int> _lastNumberForBase = new Dictionary<int, int>();

	public string Allocate(string baseRoadNumber, string identRoadNumber, bool forceSequential = false)
	{
		if (string.IsNullOrEmpty(identRoadNumber))
		{
			return Generate(baseRoadNumber, forceSequential);
		}
		ParseRoadNumber(identRoadNumber, out var number, out var prefix, out var suffix);
		string prefixSuffix = CombinePrefixSuffix(prefix, suffix);
		Reserve(number, prefixSuffix);
		return identRoadNumber;
	}

	private static string CombinePrefixSuffix(string prefix, string suffix)
	{
		return prefix + "|" + suffix;
	}

	private static void ParseRoadNumber(string roadNumber, out int number, out string prefix, out string suffix)
	{
		if (!TryParseRoadNumber(roadNumber, out number, out prefix, out suffix))
		{
			throw new Exception("Can't parse road number: " + roadNumber);
		}
	}

	private static bool TryParseRoadNumber(string roadNumber, out int number, out string prefix, out string suffix)
	{
		Match match = Regex.Match(roadNumber, "^([A-Z]*)(\\d+)([A-Z]*)$", RegexOptions.None);
		if (match.Groups.Count < 4)
		{
			Debug.LogWarning("Failed to parse road number: " + roadNumber);
			number = -1;
			prefix = null;
			suffix = null;
			return false;
		}
		prefix = match.Groups[1].Value;
		number = int.Parse(match.Groups[2].Value);
		suffix = match.Groups[3].Value;
		return true;
	}

	private static int LCG(int x0, int a, int c, int m)
	{
		return (a * x0 + c) % m;
	}

	private string Generate(string baseRoadNumberString, bool forceSequential)
	{
		ParseRoadNumber(baseRoadNumberString, out var number, out var prefix, out var suffix);
		int i;
		if (number < 1000 || forceSequential)
		{
			if (!_lastNumberForBase.TryGetValue(number, out var value))
			{
				value = Mathf.Max(0, number - 1);
			}
			for (i = value + 1; IsInUse(i, prefix, suffix); i++)
			{
			}
		}
		else
		{
			RangeForDescriptorRoadNumber(number, out var mult, out var modulus);
			if (!_lastNumberForBase.TryGetValue(number, out var value2))
			{
				value2 = number + UnityEngine.Random.Range(1, modulus);
			}
			i = LCG(value2 - number, mult, 0, modulus) + number;
			while (IsInUse(i, prefix, suffix))
			{
				i = LCG(i - number, mult, 0, modulus) + number;
				if (i == value2)
				{
					throw new Exception("Couldn't find unused number");
				}
			}
		}
		string prefixSuffix = CombinePrefixSuffix(prefix, suffix);
		Reserve(i, prefixSuffix);
		_lastNumberForBase[number] = i;
		return i.ToString();
	}

	private bool IsInUse(int roadNumber, string prefix, string suffix)
	{
		if (!_used.TryGetValue(roadNumber, out var value))
		{
			return false;
		}
		string item = CombinePrefixSuffix(prefix, suffix);
		return value.Prefixes.Contains(item);
	}

	private static void RangeForDescriptorRoadNumber(int baseRoadNumber, out int mult, out int modulus)
	{
		(int, int) tuple;
		if (baseRoadNumber >= 1000)
		{
			tuple = ((baseRoadNumber >= 10000) ? (11, 9973) : (7, 997));
		}
		else
		{
			if (baseRoadNumber < 100)
			{
				throw new ArgumentException();
			}
			tuple = (5, 97);
		}
		(mult, modulus) = tuple;
	}

	public void Release(string carRoadNumber)
	{
		ParseRoadNumber(carRoadNumber, out var number, out var prefix, out var suffix);
		if (_used.TryGetValue(number, out var value))
		{
			string item = CombinePrefixSuffix(prefix, suffix);
			value.Prefixes.Remove(item);
			if (value.Prefixes.Count == 0)
			{
				_used.Remove(number);
			}
		}
	}

	private void Reserve(int roadNumber, string prefixSuffix)
	{
		if (_used.TryGetValue(roadNumber, out var value))
		{
			if (value.Prefixes.Contains(prefixSuffix))
			{
				throw new Exception($"Road number {roadNumber} is already allocated");
			}
			value.Prefixes.Add(prefixSuffix);
		}
		else
		{
			value = new Record
			{
				Prefixes = new HashSet<string> { prefixSuffix }
			};
		}
		_used[roadNumber] = value;
	}

	public static bool ValidateRoadNumber(string roadNumber)
	{
		int number;
		string prefix;
		string suffix;
		return TryParseRoadNumber(roadNumber, out number, out prefix, out suffix);
	}
}
