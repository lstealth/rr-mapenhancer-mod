using Helpers;
using Track;
using UnityEngine;

public static class TrackHitExtension
{
	public static Location? LocationFromMouse(this Graph graph, Camera camera)
	{
		Ray ray = camera.ScreenPointToRay(Input.mousePosition);
		if (!Physics.Raycast(ray, out var hitInfo, float.PositiveInfinity, 1 << Layers.Track))
		{
			return null;
		}
		if (!graph.TryGetLocationFromWorldPoint(hitInfo.point, 2f, out var output))
		{
			return null;
		}
		Graph.PositionDirection positionDirection = graph.GetPositionDirection(output);
		if ((double)Vector3.Dot(positionDirection.Direction, (positionDirection.Position - WorldTransformer.WorldToGame(ray.origin)).normalized) > 0.0)
		{
			output = output.Flipped();
		}
		return output;
	}
}
