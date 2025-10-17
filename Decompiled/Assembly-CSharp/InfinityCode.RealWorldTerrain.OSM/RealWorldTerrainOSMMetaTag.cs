using System;

namespace InfinityCode.RealWorldTerrain.OSM;

[Serializable]
public class RealWorldTerrainOSMMetaTag
{
	public string title;

	public string info;

	public bool CompareKeyOrValue(string value, bool searchInKey, bool searchInValue)
	{
		if (!searchInKey || !CompareString(title, value))
		{
			if (searchInValue)
			{
				return CompareString(info, value);
			}
			return false;
		}
		return true;
	}

	private bool CompareString(string v1, string v2)
	{
		if (v1 == null || v2 == null)
		{
			return false;
		}
		if (v2.Length > v1.Length)
		{
			return false;
		}
		for (int i = 0; i < v1.Length - v2.Length + 1; i++)
		{
			bool flag = true;
			for (int j = 0; j < v2.Length; j++)
			{
				char num = char.ToUpperInvariant(v1[i + j]);
				char c = v2[j];
				if (num != c)
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				return true;
			}
		}
		return false;
	}
}
