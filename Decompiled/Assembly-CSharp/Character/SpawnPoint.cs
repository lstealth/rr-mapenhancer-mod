using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using UnityEngine;

namespace Character;

public class SpawnPoint : MonoBehaviour
{
	public int priority;

	public float radius = 3f;

	public static IEnumerable<SpawnPoint> All
	{
		get
		{
			SpawnPoint[] array = UnityEngine.Object.FindObjectsOfType<SpawnPoint>();
			Array.Sort(array, (SpawnPoint a, SpawnPoint b) => b.priority.CompareTo(a.priority));
			return array;
		}
	}

	public (Vector3, Quaternion) GamePositionRotation => (WorldTransformer.WorldToGame(base.transform.position), base.transform.rotation);

	public static SpawnPoint Default
	{
		get
		{
			SpawnPoint spawnPoint = All.FirstOrDefault();
			if (spawnPoint == null)
			{
				throw new Exception("No spawn points found");
			}
			return spawnPoint;
		}
	}

	public static SpawnPoint ClosestTo(Vector3 worldPosition)
	{
		return All.OrderBy((SpawnPoint s) => Vector3.Distance(s.transform.position, worldPosition)).First();
	}
}
