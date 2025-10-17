using System.Collections.Generic;
using Helpers;
using UnityEngine;

namespace Model.Ops;

public class Area : MonoBehaviour
{
	public string identifier;

	public float radius;

	public Color tagColor;

	public IEnumerable<Industry> Industries => GetComponentsInChildren<Industry>();

	private void OnDrawGizmosSelected()
	{
	}

	public bool Contains(OpsCarPosition position)
	{
		foreach (Industry industry in Industries)
		{
			if (industry.Contains(position))
			{
				return true;
			}
		}
		return false;
	}

	public bool Contains(Vector3 point)
	{
		return Vector3.Distance(WorldTransformer.WorldToGame(base.transform.position), point) < radius;
	}
}
