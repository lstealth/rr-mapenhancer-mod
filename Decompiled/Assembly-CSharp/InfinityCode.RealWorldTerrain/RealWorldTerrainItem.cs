using System;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain;

[Serializable]
[AddComponentMenu("")]
public class RealWorldTerrainItem : RealWorldTerrainMonoBase
{
	public RealWorldTerrainContainer container;

	public int x;

	public int y;

	public int ry;

	public bool needUpdate;

	public MeshFilter meshFilter;

	public Terrain terrain;

	public Texture2D texture
	{
		get
		{
			if (prefs.resultType == RealWorldTerrainResultType.terrain)
			{
				if (terrainData == null || terrainData.terrainLayers.Length == 0)
				{
					return null;
				}
				TerrainLayer terrainLayer = terrainData.terrainLayers[0];
				if (!(terrainLayer != null))
				{
					return null;
				}
				return terrainLayer.diffuseTexture;
			}
			MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
			if (meshRenderer == null)
			{
				meshRenderer = GetComponentInChildren<MeshRenderer>();
				if (meshRenderer == null)
				{
					return null;
				}
			}
			if (!(meshRenderer.sharedMaterial != null))
			{
				return null;
			}
			return meshRenderer.sharedMaterial.mainTexture as Texture2D;
		}
		set
		{
			if (prefs.resultType == RealWorldTerrainResultType.terrain)
			{
				if (terrainData == null)
				{
					return;
				}
				TerrainLayer[] terrainLayers = terrainData.terrainLayers;
				if (terrainLayers.Length != 0)
				{
					if (terrainLayers[0] == null)
					{
						terrainLayers[0] = new TerrainLayer
						{
							tileSize = new Vector2(terrainData.size.x, terrainData.size.z)
						};
					}
					terrainLayers[0].diffuseTexture = value;
					terrainData.terrainLayers = terrainLayers;
				}
				return;
			}
			MeshRenderer component = GetComponent<MeshRenderer>();
			if (component != null)
			{
				component.sharedMaterial.mainTexture = value;
				return;
			}
			MeshRenderer[] componentsInChildren = GetComponentsInChildren<MeshRenderer>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].sharedMaterial.mainTexture = value;
			}
		}
	}

	public TerrainData terrainData
	{
		get
		{
			if (!(terrain != null))
			{
				return null;
			}
			return terrain.terrainData;
		}
	}

	public float GetHeightmapValueByMercator(double mx, double my)
	{
		float num = (float)((mx - leftMercator) / (rightMercator - leftMercator));
		float num2 = (float)((my - topMercator) / (bottomMercator - topMercator));
		return terrainData.GetInterpolatedHeight(num, 1f - num2);
	}

	public override RealWorldTerrainItem GetItemByWorldPosition(Vector3 worldPosition)
	{
		return container.GetItemByWorldPosition(worldPosition);
	}

	public override bool GetWorldPosition(double lng, double lat, out Vector3 worldPosition)
	{
		worldPosition = default(Vector3);
		if (!Contains(lng, lat))
		{
			Debug.Log("Wrong coordinates");
			return false;
		}
		Bounds bounds = new Bounds(base.bounds.center + container.transform.position, base.bounds.size);
		RealWorldTerrainUtils.LatLongToMercat(lng, lat, out var mx, out var my);
		double num = RealWorldTerrainUtils.Clamp((mx - leftMercator) / width, 0.0, 1.0);
		double num2 = RealWorldTerrainUtils.Clamp(1.0 - (my - topMercator) / height, 0.0, 1.0);
		double num3 = (double)bounds.size.x * num + (double)bounds.min.x;
		double num4 = (double)bounds.size.z * num2 + (double)bounds.min.z;
		if (prefs.resultType == RealWorldTerrainResultType.terrain)
		{
			if (terrain == null)
			{
				return false;
			}
			TerrainData terrainData = this.terrainData;
			Vector3 vector = terrainData.size;
			Vector3 position = base.transform.position;
			if ((double)position.x > num3 || (double)position.z > num4 || (double)(position.x + vector.x) < num3 || (double)(position.z + vector.z) < num4)
			{
				return false;
			}
			double num5 = (num3 - (double)terrain.gameObject.transform.position.x) / (double)terrainData.size.x;
			double num6 = (num4 - (double)terrain.gameObject.transform.position.z) / (double)terrainData.size.z;
			double num7 = terrainData.GetInterpolatedHeight((float)num5, (float)num6) + position.y;
			worldPosition.x = (float)num3;
			worldPosition.y = (float)num7;
			worldPosition.z = (float)num4;
		}
		else
		{
			if (!((double)bounds.min.x <= num3) || !((double)bounds.min.z <= num4) || !((double)bounds.max.x >= num3) || !((double)bounds.max.z >= num4))
			{
				return false;
			}
			float num8 = 0f;
			RaycastHit[] array = Physics.RaycastAll(new Vector3((float)num3, base.bounds.max.y + 10f, (float)num4), Vector3.down, float.MaxValue);
			for (int i = 0; i < array.Length; i++)
			{
				RaycastHit raycastHit = array[i];
				if (raycastHit.transform.gameObject.GetComponentInParent<RealWorldTerrainItem>() != null)
				{
					num8 = raycastHit.point.y;
					break;
				}
			}
			worldPosition.x = (float)num3;
			worldPosition.y = num8;
			worldPosition.z = (float)num4;
		}
		return true;
	}

	public override bool GetWorldPosition(Vector2 coordinates, out Vector3 worldPosition)
	{
		return GetWorldPosition(coordinates.x, coordinates.y, out worldPosition);
	}
}
