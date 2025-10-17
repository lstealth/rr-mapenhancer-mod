using UnityEngine;

namespace Helpers;

public static class MaterialInstancesExtensions
{
	public static Material CreateUniqueMaterial(this Renderer renderer)
	{
		return renderer.material = new Material(renderer.sharedMaterial);
	}
}
