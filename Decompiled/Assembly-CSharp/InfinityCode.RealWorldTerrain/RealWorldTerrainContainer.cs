using System;
using System.Linq;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain;

[Serializable]
[AddComponentMenu("")]
public class RealWorldTerrainContainer : RealWorldTerrainMonoBase
{
	public float billboardStart = 50f;

	public float detailDensity = 1f;

	public float detailDistance = 80f;

	public string folder;

	public RealWorldTerrainVector2i terrainCount;

	public string title;

	public float treeDistance = 2000f;

	public RealWorldTerrainItem[] terrains;

	public static RealWorldTerrainContainer[] GetInstances()
	{
		return UnityEngine.Object.FindObjectsOfType<RealWorldTerrainContainer>().ToArray();
	}

	public override RealWorldTerrainItem GetItemByWorldPosition(Vector3 worldPosition)
	{
		for (int i = 0; i < terrains.Length; i++)
		{
			RealWorldTerrainItem realWorldTerrainItem = terrains[i];
			if (!(realWorldTerrainItem == null))
			{
				Bounds bounds = new Bounds(realWorldTerrainItem.bounds.center + base.transform.position, realWorldTerrainItem.bounds.size);
				if (bounds.min.x <= worldPosition.x && bounds.min.z <= worldPosition.z && bounds.max.x >= worldPosition.x && bounds.max.z >= worldPosition.z)
				{
					return realWorldTerrainItem;
				}
			}
		}
		return null;
	}

	public override bool GetWorldPosition(double lng, double lat, out Vector3 worldPosition)
	{
		worldPosition = default(Vector3);
		if (!Contains(lng, lat))
		{
			Debug.Log("Wrong coordinates");
			return false;
		}
		if (terrains == null || terrains.Length == 0)
		{
			return false;
		}
		RealWorldTerrainUtils.LatLongToMercat(lng, lat, out var mx, out var my);
		double num = RealWorldTerrainUtils.Clamp((mx - leftMercator) / (rightMercator - leftMercator), 0.0, 1.0);
		double num2 = RealWorldTerrainUtils.Clamp(1.0 - (my - topMercator) / (bottomMercator - topMercator), 0.0, 1.0);
		Bounds bounds = new Bounds(base.bounds.center + base.transform.position, base.bounds.size);
		double num3 = (double)bounds.size.x * num + (double)bounds.min.x;
		double num4 = (double)bounds.size.z * num2 + (double)bounds.min.z;
		if (prefs.resultType == RealWorldTerrainResultType.terrain)
		{
			Terrain terrain = null;
			for (int i = 0; i < terrains.Length; i++)
			{
				RealWorldTerrainItem realWorldTerrainItem = terrains[i];
				Bounds bounds2 = new Bounds(realWorldTerrainItem.bounds.center + base.transform.position, realWorldTerrainItem.bounds.size);
				if ((double)bounds2.min.x <= num3 && (double)bounds2.min.z <= num4 && (double)bounds2.max.x >= num3 && (double)bounds2.max.z >= num4)
				{
					terrain = realWorldTerrainItem.terrain;
					break;
				}
			}
			if (terrain == null)
			{
				return false;
			}
			double num5 = (num3 - (double)terrain.gameObject.transform.position.x) / (double)terrain.terrainData.size.x;
			double num6 = (num4 - (double)terrain.gameObject.transform.position.z) / (double)terrain.terrainData.size.z;
			double num7 = terrain.terrainData.GetInterpolatedHeight((float)num5, (float)num6) + terrain.gameObject.transform.position.y;
			worldPosition.x = (float)num3;
			worldPosition.y = (float)num7;
			worldPosition.z = (float)num4;
		}
		else if (prefs.resultType == RealWorldTerrainResultType.mesh)
		{
			bool flag = false;
			for (int j = 0; j < terrains.Length; j++)
			{
				RealWorldTerrainItem realWorldTerrainItem2 = terrains[j];
				Bounds bounds3 = new Bounds(realWorldTerrainItem2.bounds.center + base.transform.position, realWorldTerrainItem2.bounds.size);
				if (!((double)bounds3.min.x <= num3) || !((double)bounds3.min.z <= num4) || !((double)bounds3.max.x >= num3) || !((double)bounds3.max.z >= num4))
				{
					continue;
				}
				float y = 0f;
				RaycastHit[] array = Physics.RaycastAll(new Vector3((float)num3, realWorldTerrainItem2.bounds.max.y + 10f, (float)num4), Vector3.down, float.MaxValue);
				for (int k = 0; k < array.Length; k++)
				{
					RaycastHit raycastHit = array[k];
					if (raycastHit.transform.gameObject.GetComponentInParent<RealWorldTerrainItem>() != null)
					{
						y = raycastHit.point.y;
						break;
					}
				}
				worldPosition.x = (float)num3;
				worldPosition.y = y;
				worldPosition.z = (float)num4;
				flag = true;
				break;
			}
			if (!flag)
			{
				return false;
			}
		}
		return true;
	}

	public override bool GetWorldPosition(Vector2 coordinates, out Vector3 worldPosition)
	{
		return GetWorldPosition(coordinates.x, coordinates.y, out worldPosition);
	}
}
