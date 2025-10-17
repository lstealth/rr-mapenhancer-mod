using System.Linq;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain;

public class RealWorldTerrainLookLngLat : MonoBehaviour
{
	public float distance = 10f;

	public float height = 5f;

	public float lat;

	public float lng;

	public static bool GetRealWorldPoint(out Vector3 position, double lng, double lat)
	{
		position = default(Vector3);
		RealWorldTerrainContainer realWorldTerrainContainer = Object.FindObjectsOfType<RealWorldTerrainContainer>().FirstOrDefault((RealWorldTerrainContainer t) => t.Contains(lng, lat));
		if (realWorldTerrainContainer == null)
		{
			Debug.Log("Target not found");
			return false;
		}
		return realWorldTerrainContainer.GetWorldPosition(lng, lat, out position);
	}

	public static void LookTo(float lng, float lat)
	{
		if (GetRealWorldPoint(out var position, lng, lat))
		{
			Camera.main.transform.LookAt(position);
		}
	}

	public static void MoveTo(float lng, float lat, float distance, float height)
	{
		if (GetRealWorldPoint(out var position, lng, lat))
		{
			Vector3 vector = Camera.main.transform.position - position;
			vector.y = 0f;
			Vector3 position2 = position + vector.normalized * distance;
			position2.y += height;
			Camera.main.transform.position = position2;
			Camera.main.transform.LookAt(position);
		}
	}
}
