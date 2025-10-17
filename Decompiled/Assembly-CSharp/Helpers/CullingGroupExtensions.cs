using Serilog;
using UnityEngine;

namespace Helpers;

public static class CullingGroupExtensions
{
	public static int CalculateDistanceBand(this CullingGroup cullingGroup, Vector3 position, float[] distanceBands)
	{
		if ((object)cullingGroup.targetCamera == null)
		{
			if (Application.isPlaying)
			{
				Log.Warning("targetCamera on culling group is null");
			}
			else
			{
				Log.Verbose("targetCamera on culling group is null");
			}
			return 0;
		}
		int i = 0;
		for (float num = Vector3.Distance(cullingGroup.targetCamera.transform.position, position); i < distanceBands.Length && !(num < distanceBands[i]); i++)
		{
		}
		return i;
	}

	public static void AutoAssignTargetCamera(this CullingGroup cullingGroup, Object owner)
	{
		AutoAssignTargetCameraPlayMode(cullingGroup);
	}

	private static void AutoAssignTargetCameraPlayMode(CullingGroup cullingGroup)
	{
		Camera camera = (cullingGroup.targetCamera = Camera.main);
		cullingGroup.SetDistanceReferencePoint(camera.transform);
	}
}
