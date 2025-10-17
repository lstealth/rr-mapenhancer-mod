using System;
using System.Collections.Generic;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain;

[Serializable]
public class RealWorldTerrainPrefsBase
{
	public float buildingBasementDepth;

	public RealWorldTerrainBuildingBottomMode buildingBottomMode;

	public float buildingFloorHeight = 3.5f;

	public RealWorldTerrainRangeI buildingFloorLimits = new RealWorldTerrainRangeI(5, 7, 1, 50);

	public int buildingGenerator;

	public List<RealWorldTerrainBuildingMaterial> buildingMaterials;

	public List<RealWorldTerrainBuildingPrefab> buildingPrefabs;

	public bool buildingSaveInResult = true;

	public bool buildingSingleRequest = true;

	public bool buildingUseColorTags;

	public RealWorldTerrainBuildR2Collider buildRCollider;

	public bool buildR3Collider;

	public RealWorldTerrainBuildR2RenderMode buildRRenderMode = RealWorldTerrainBuildR2RenderMode.full;

	public List<RealWorldTerrainBuildR2Material> buildR2Materials;

	public List<RealWorldTerrainBuildR3Material> buildR3Materials;

	public int customBuildRGeneratorStyle;

	public int customBuildRGeneratorTexturePack;

	public RealWorldTerrainBuildRPresetsItem[] customBuildRPresets;

	public bool dynamicBuildings = true;

	public List<RealWorldTerrainPOI> POI;

	public string riverEngine = "Built-In";

	public Material riverMaterial;

	public string title;

	public int grassDensity = 100;

	public string grassEngine;

	public List<Texture2D> grassPrefabs;

	public List<int> vegetationStudioGrassTypes;

	public RealWorldTerrainVolumeGrassOutsidePoints volumeGrassOutsidePoints;

	public string[] erRoadTypes;

	public bool erGenerateConnection = true;

	public bool erSnapToTerrain = true;

	public float erWidthMultiplier = 1f;

	public string roadEngine;

	public RealWorldTerrainRoadType roadTypes = (RealWorldTerrainRoadType)(-1);

	public RealWorldTerrainRoadTypeMode roadTypeMode;

	public Material splineBendMaterial;

	public Mesh splineBendMesh;

	public bool alignWaterLine;

	public Vector2 autoDetectElevationOffset = new Vector2(100f, 100f);

	public int baseMapResolution = 1024;

	public bool bingMapsUseZeroAsUnknown;

	public int controlTextureResolution = 512;

	public float depthSharpness;

	public int detailResolution = 2048;

	public RealWorldTerrainElevationProvider elevationProvider;

	public RealWorldTerrainElevationRange elevationRange;

	public RealWorldTerrainElevationType elevationType;

	public Vector3 fixedTerrainSize = new Vector3(500f, 600f, 500f);

	public int gaiaStampResolution = 1024;

	public bool generateUnderWater;

	public int heightmapResolution = 129;

	public bool ignoreSRTMErrors;

	public float fixedMaxElevation = 1000f;

	public float fixedMinElevation;

	public RealWorldTerrainMaxElevation maxElevationType;

	public short nodataValue;

	public RealWorldTerrainByteOrder rawByteOrder;

	public string rawFilename = "terrain";

	public int rawHeight = 1024;

	public int rawWidth = 1024;

	public RealWorldTerrainRawType rawType;

	public int resolutionPerPatch = 16;

	public RealWorldTerrainResultType resultType;

	public int sizeType;

	public Vector3 terrainScale = Vector3.one;

	public Texture2D waterDetectionTexture;

	public int hugeTexturePageSize = 2048;

	public int hugeTextureRows = 13;

	public int hugeTextureCols = 13;

	public string mapTypeID;

	public string mapTypeExtraFields;

	public int maxTextureLevel;

	public bool reduceTextures = true;

	public RealWorldTerrainVector2i textureCount = RealWorldTerrainVector2i.one;

	public RealWorldTerrainTextureFileType textureFileType = RealWorldTerrainTextureFileType.jpg;

	public int textureFileQuality = 100;

	public RealWorldTerrainTextureProvider textureProvider = RealWorldTerrainTextureProvider.virtualEarth;

	public string textureProviderURL = "http://localhost/tiles/{zoom}/{x}/{y}";

	public RealWorldTerrainVector2i textureSize = new RealWorldTerrainVector2i(1024, 1024);

	public bool textureMipMaps;

	public RealWorldTerrainTextureResultType textureResultType;

	public RealWorldTerrainTextureType textureType;

	public List<TerrainLayer> vectorTerrainBaseLayers;

	public Vector2 vectorTerrainBaseLayersNoiseOffset = Vector2.zero;

	public float vectorTerrainBaseLayersNoiseScale = 16f;

	public List<RealWorldTerrainVectorTerrainLayerFeature> vectorTerrainLayers;

	public int treeDensity = 100;

	public string treeEngine;

	public List<GameObject> treePrefabs;

	public List<int> vegetationStudioTreeTypes;
}
