using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace WorldStreamer2;

public class TerrainManagerSettings : MonoBehaviour
{
	public int splitSize = 2;

	public string terrainsDataPath = "/NatureManufacture Assets/WorldStreamer/TerrainsData/";

	public bool addTerrainCulling = true;

	public float trainglesTreesMax = 65000f;

	public int basemapResolution = 512;

	public bool useTerrainNormal = true;

	public int terrainNormalDetails = 4;

	public float terrainNormalStrength = 1f;

	public bool useTextureNormal = true;

	public bool useSmoothness = true;

	public int terrainsCount = 16;

	public int terrainLod = 3;

	public bool useBaseMap = true;

	public bool addVerticesDown = true;

	public float verticesDownDistance = 5f;

	public float yOffset = -0.4f;

	public string terrainPrefixName = "LOD1_";

	public Color ambientLightColor = new Color(1.1f, 1.1f, 1.1f);

	public string terrainPath = "/NatureManufacture Assets/WorldStreamer/TerrainMeshes/";

	public List<TerrainTrees> terrainTrees = new List<TerrainTrees>();

	public ColorSpace colorSpaceLast = ColorSpace.Uninitialized;

	public bool showBasicTerrainSettings;

	public int groupingID;

	public bool allowAutoConnect = true;

	public bool drawHeightmap = true;

	public bool drawInstanced = true;

	public float heightmapPixelError = 5f;

	public float basemapDistance = 1000f;

	public ShadowCastingMode shadowCastingMode = ShadowCastingMode.TwoSided;

	public ReflectionProbeUsage reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;

	public Material materialTemplate;

	public bool showTreeAndDetailSettings;

	public bool drawTreesAndFoliage = true;

	public bool bakeLightProbesForTrees = true;

	public bool deringLightProbesForTrees = true;

	public bool preserveTreePrototypeLayers;

	public float detailObjectDistance = 150f;

	public float detailObjectDensity = 1f;

	public float treeDistance = 2000f;

	public float treeBillboardDistance = 50f;

	public float treeCrossFadeLength = 5f;

	public int treeMaximumFullLODCount = 50;

	public bool showGrassWindSettings;

	public float wavingGrassStrength = 0.5f;

	public float wavingGrassSpeed = 0.5f;

	public float wavingGrassAmount = 0.167f;

	public Color wavingGrassTint = new Color(1f, 1f, 1f, 0f);
}
