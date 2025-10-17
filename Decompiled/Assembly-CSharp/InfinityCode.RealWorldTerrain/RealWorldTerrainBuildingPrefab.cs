using System;
using System.Collections.Generic;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain;

[Serializable]
public class RealWorldTerrainBuildingPrefab
{
	[Serializable]
	public class OSMTag
	{
		public string key;

		public string value;

		public bool hasEmptyKey => string.IsNullOrEmpty(key);

		public bool hasEmptyValue => string.IsNullOrEmpty(value);

		public bool isEmpty
		{
			get
			{
				if (hasEmptyKey)
				{
					return hasEmptyValue;
				}
				return false;
			}
		}
	}

	public enum SizeMode
	{
		originalSize,
		fitToBounds
	}

	public enum HeightMode
	{
		original,
		averageXZ,
		levelBased,
		fixedHeight
	}

	public enum PlacementMode
	{
		lowerCorner,
		highestCorner,
		average
	}

	public GameObject prefab;

	public List<OSMTag> tags;

	public SizeMode sizeMode = SizeMode.fitToBounds;

	public HeightMode heightMode = HeightMode.levelBased;

	public PlacementMode placementMode;

	public float fixedHeight = 15f;

	public bool hasBounds => prefab.GetComponent<Collider>() != null;
}
