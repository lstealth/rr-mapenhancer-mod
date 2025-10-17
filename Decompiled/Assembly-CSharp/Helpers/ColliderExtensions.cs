using System;
using UnityEngine;

namespace Helpers;

public static class ColliderExtensions
{
	public static (Vector3, Vector3) StartEndPoints(this CapsuleCollider capsuleCollider, float additionalHeight = 0f)
	{
		Vector3 vector = new Vector3(0f, capsuleCollider.height / 2f + additionalHeight, 0f);
		Vector3 position = capsuleCollider.transform.position;
		Vector3 center = capsuleCollider.center;
		Vector3 item = position + vector + center;
		Vector3 item2 = position - vector + center;
		return (item, item2);
	}

	public static (Vector3 startPos, Vector3 endPos, float radius) GetCapsuleSphereLineSegment(this Collider coll)
	{
		if (!(coll is CapsuleCollider capsuleCollider))
		{
			if (coll is SphereCollider sphereCollider)
			{
				Vector3 vector = sphereCollider.transform.position + sphereCollider.center;
				return (startPos: vector + Vector3.down, endPos: vector + Vector3.up, radius: sphereCollider.radius);
			}
			throw new ArgumentException($"Collider not supported: {coll}");
		}
		var (item, item2) = capsuleCollider.StartEndPoints();
		return (startPos: item, endPos: item2, radius: capsuleCollider.radius);
	}
}
