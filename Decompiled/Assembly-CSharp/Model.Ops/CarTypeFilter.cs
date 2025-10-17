using System;
using System.Collections.Generic;
using UnityEngine;

namespace Model.Ops;

[Serializable]
public class CarTypeFilter
{
	[SerializeField]
	public string queryString;

	private HashSet<string> _prefixes;

	private HashSet<string> _exact;

	public bool IsEmpty => string.IsNullOrWhiteSpace(queryString);

	public CarTypeFilter(string queryString)
	{
		this.queryString = queryString ?? "";
	}

	public override string ToString()
	{
		return queryString;
	}

	public bool Matches(string carType)
	{
		BuildIfNeeded();
		foreach (string item in _exact)
		{
			if (carType.Equals(item, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		foreach (string prefix in _prefixes)
		{
			if (carType.StartsWith(prefix))
			{
				return true;
			}
		}
		return false;
	}

	private void BuildIfNeeded()
	{
		if (_prefixes != null)
		{
			return;
		}
		string[] array = queryString.Split(',');
		HashSet<string> hashSet = new HashSet<string>();
		HashSet<string> hashSet2 = new HashSet<string>();
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (text.EndsWith("*"))
			{
				hashSet.Add(text.Remove(text.Length - 1));
			}
			else
			{
				hashSet2.Add(text);
			}
		}
		_prefixes = hashSet;
		_exact = hashSet2;
	}

	public bool Overlaps(CarTypeFilter other)
	{
		if (other == null)
		{
			return true;
		}
		BuildIfNeeded();
		other.BuildIfNeeded();
		if (!_prefixes.Overlaps(other._prefixes))
		{
			return _exact.Overlaps(other._exact);
		}
		return true;
	}

	public bool Equals(CarTypeFilter other)
	{
		BuildIfNeeded();
		other.BuildIfNeeded();
		if (_prefixes.SetEquals(other._prefixes))
		{
			return _exact.SetEquals(other._exact);
		}
		return false;
	}
}
