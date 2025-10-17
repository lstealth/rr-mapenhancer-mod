using System.Collections.Generic;
using System.Linq;

namespace InfinityCode.RealWorldTerrain.OSM;

public class RealWorldTerrainOSMBase
{
	public string id;

	public List<RealWorldTerrainOSMTag> tags;

	public bool Equals(RealWorldTerrainOSMBase other)
	{
		if (other == null)
		{
			return false;
		}
		if (this == other)
		{
			return true;
		}
		return id == other.id;
	}

	public override int GetHashCode()
	{
		return id.GetHashCode();
	}

	public string GetTagValue(string key)
	{
		List<RealWorldTerrainOSMTag> list = tags.Where((RealWorldTerrainOSMTag tag) => tag.key == key).ToList();
		if (list.Count > 0)
		{
			return list[0].value;
		}
		return string.Empty;
	}

	public bool HasTag(string key, string value)
	{
		return tags.Any((RealWorldTerrainOSMTag t) => t.key == key && t.value == value);
	}

	public bool HasTagKey(params string[] keys)
	{
		return keys.Any((string key) => tags.Any((RealWorldTerrainOSMTag t) => t.key == key));
	}

	public bool HasTagValue(params string[] values)
	{
		return values.Any((string val) => tags.Any((RealWorldTerrainOSMTag t) => t.value == val));
	}

	public bool HasTags(string key, params string[] values)
	{
		return tags.Any((RealWorldTerrainOSMTag tag) => tag.key == key && values.Any((string v) => v == tag.value));
	}
}
