using System;
using UnityEngine;

namespace Helpers;

[Serializable]
public struct Capsule
{
	public Vector3 a;

	public Vector3 b;

	public float radius;

	public Capsule(Vector3 a, Vector3 b, float radius)
	{
		this.a = a;
		this.b = b;
		this.radius = radius;
	}
}
