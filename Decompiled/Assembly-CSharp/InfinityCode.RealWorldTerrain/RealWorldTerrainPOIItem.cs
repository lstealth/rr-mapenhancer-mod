using UnityEngine;

namespace InfinityCode.RealWorldTerrain;

public class RealWorldTerrainPOIItem : MonoBehaviour
{
	public string title;

	public double x;

	public double y;

	public float altitude;

	public void SetPrefs(RealWorldTerrainPOI poi)
	{
		title = poi.title;
		x = poi.x;
		y = poi.y;
		altitude = poi.altitude;
	}
}
