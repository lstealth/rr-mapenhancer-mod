using System;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain;

[Serializable]
public class RealWorldTerrainBuildingMaterial
{
	public Material roof;

	public Material wall;

	public Vector2 tileSize = new Vector2(30f, 30f);
}
