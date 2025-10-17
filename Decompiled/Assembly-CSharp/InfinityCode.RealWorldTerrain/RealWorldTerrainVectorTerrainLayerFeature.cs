using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain;

[Serializable]
public class RealWorldTerrainVectorTerrainLayerFeature
{
	[Serializable]
	public class Rule
	{
		public RealWorldTerrainMapboxLayer layer;

		public int extra = -1;

		[NonSerialized]
		private string _layerName;

		public string layerName
		{
			get
			{
				if (_layerName == null)
				{
					_layerName = layer.ToString();
				}
				return _layerName;
			}
		}

		public bool hasExtra
		{
			get
			{
				if (layer != RealWorldTerrainMapboxLayer.landuse_overlay && layer != RealWorldTerrainMapboxLayer.landuse && layer != RealWorldTerrainMapboxLayer.waterway)
				{
					return layer == RealWorldTerrainMapboxLayer.structure;
				}
				return true;
			}
		}
	}

	public const float TerrainLayerLineHeight = 20f;

	private static string[] _layerNames;

	private static List<string> _landuseNames;

	private static List<string> _landuseOverlayNames;

	private static List<string> _structureNames;

	private static List<string> _waterwayNames;

	public List<TerrainLayer> terrainLayers;

	public Vector2 noiseOffset = Vector2.zero;

	public float noiseScale = 16f;

	public List<Rule> rules;

	[NonSerialized]
	private float? _height;

	public float height
	{
		get
		{
			if (!_height.HasValue)
			{
				UpdateHeight();
			}
			return _height.Value;
		}
	}

	public static List<string> landuseNames
	{
		get
		{
			if (_landuseNames == null)
			{
				_landuseNames = Enum.GetNames(typeof(RealWorldTerrainMapboxLanduse)).ToList();
			}
			return _landuseNames;
		}
	}

	public static List<string> landuseOverlayNames
	{
		get
		{
			if (_landuseOverlayNames == null)
			{
				_landuseOverlayNames = Enum.GetNames(typeof(RealWorldTerrainMapboxLanduseOverlay)).ToList();
			}
			return _landuseOverlayNames;
		}
	}

	public static string[] layerNames
	{
		get
		{
			if (_layerNames == null)
			{
				_layerNames = Enum.GetNames(typeof(RealWorldTerrainMapboxLayer));
			}
			return _layerNames;
		}
	}

	public static List<string> structureNames
	{
		get
		{
			if (_structureNames == null)
			{
				_structureNames = Enum.GetNames(typeof(RealWorldTerrainMapboxStructure)).ToList();
			}
			return _structureNames;
		}
	}

	public static List<string> waterwayNames
	{
		get
		{
			if (_waterwayNames == null)
			{
				_waterwayNames = Enum.GetNames(typeof(RealWorldTerrainMapboxWaterway)).ToList();
			}
			return _waterwayNames;
		}
	}

	public void UpdateHeight()
	{
		int num = 3;
		num = ((terrainLayers == null) ? (num + 1) : ((terrainLayers.Count != 1) ? (num + (terrainLayers.Count + 2)) : (num + 1)));
		if (rules != null)
		{
			foreach (Rule rule in rules)
			{
				num += (rule.hasExtra ? 3 : 2);
			}
		}
		_height = (float)num * 20f + 5f;
	}
}
