using System;
using System.Collections.Generic;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain.OSM;

[Serializable]
[AddComponentMenu("")]
public class RealWorldTerrainOSMMeta : MonoBehaviour
{
	public Vector2 center;

	public bool hasURL;

	public bool hasWebsite;

	public bool hasWikipedia;

	public RealWorldTerrainOSMMetaTag[] metaInfo;

	private void AddInfo(string title, string info)
	{
		if (metaInfo == null)
		{
			metaInfo = new RealWorldTerrainOSMMetaTag[0];
		}
		List<RealWorldTerrainOSMMetaTag> list = new List<RealWorldTerrainOSMMetaTag>(metaInfo)
		{
			new RealWorldTerrainOSMMetaTag
			{
				info = info,
				title = title
			}
		};
		switch (title)
		{
		case "url":
			hasURL = true;
			break;
		case "website":
			hasWebsite = true;
			break;
		case "wikipedia":
			hasWikipedia = true;
			break;
		}
		metaInfo = list.ToArray();
	}

	public bool ContainKeyOrValue(string tag, bool searchInKey, bool searchInValue)
	{
		if (metaInfo == null)
		{
			return false;
		}
		for (int i = 0; i < metaInfo.Length; i++)
		{
			if (metaInfo[i].CompareKeyOrValue(tag, searchInKey, searchInValue))
			{
				return true;
			}
		}
		return false;
	}

	public RealWorldTerrainOSMMeta GetFromOSM(RealWorldTerrainOSMBase item, Vector2 center = default(Vector2))
	{
		foreach (RealWorldTerrainOSMTag tag in item.tags)
		{
			AddInfo(tag.key, tag.value);
		}
		this.center = center;
		return this;
	}
}
