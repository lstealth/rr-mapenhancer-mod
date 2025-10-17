using System;
using Core;
using Helpers;
using Map.Runtime;
using Map.Runtime.MapModifiers;
using Map.Runtime.MaskComponents;
using UnityEngine;

namespace Track;

public static class RoadbedBuilder
{
	public static void BuildMasks(BezierCurve curve, GameObject parent, TrackSegment.Style style, string key)
	{
		StaticMapMask staticMapMask = parent.AddComponent<StaticMapMask>();
		staticMapMask.CoordinateSystem = MapManager.CoordinateSystem.Game;
		switch (style)
		{
		case TrackSegment.Style.Standard:
		{
			Vector3 o2 = new Vector3(0f, -0.2f, 0f);
			staticMapMask.AddModifier(new HeightmapModifier(1, HeightmapModifierKind.Roadbed, new CurveMaskDescriptor(curve.OffsetBy(o2), 0.25f, 20f, 1f)));
			staticMapMask.AddModifier(new MaskModifier(MaskName.Track, 1f, new CurveMaskDescriptor(curve, 0.25f, 8f, 0.75f, 1.5f)));
			break;
		}
		case TrackSegment.Style.Yard:
		{
			Vector3 o = new Vector3(0f, -0.2f, 0f);
			staticMapMask.AddModifier(new HeightmapModifier(1, HeightmapModifierKind.Roadbed, new CurveMaskDescriptor(curve.OffsetBy(o), 1.5f, 20f)));
			staticMapMask.AddModifier(new MaskModifier(MaskName.Object, 1f, new CurveMaskDescriptor(curve, 4f, 4f)));
			staticMapMask.AddModifier(new MaskModifier(MaskName.Dirt, 1f, new CurveMaskDescriptor(curve, 2f, 6f)));
			break;
		}
		case TrackSegment.Style.Bridge:
			staticMapMask.AddModifier(new MaskModifier(MaskName.CutTrees, 1f, new CurveMaskDescriptor(curve, 6f, 14f)));
			break;
		case TrackSegment.Style.Tunnel:
		{
			staticMapMask.AddModifier(new TunnelModifier(new CurveMaskDescriptor(curve, 1.6f, 0f)));
			float num = curve.CalculateLength();
			float num2 = Mathf.Min(6f, num / 2f);
			curve.Split(curve.ParameterForDistance(num2, 0.1f), out var l, out var r);
			curve.Split(curve.ParameterForDistance(num - num2, 0.1f), out r, out var r2);
			staticMapMask.AddModifier(new MaskModifier(MaskName.Track, 1f, new CurveMaskDescriptor(l, 4f, 6f)));
			staticMapMask.AddModifier(new MaskModifier(MaskName.Track, 1f, new CurveMaskDescriptor(r2, 4f, 6f)));
			break;
		}
		default:
			throw new ArgumentOutOfRangeException("style", style, null);
		}
	}

	private static void ApplyTerrainCarvingDefaults(RamSpline spline)
	{
		spline.terrainCarve = AnimationCurve.Constant(0f, 10f, -0.5f);
		spline.maskCarve = 1 << Layers.Terrain;
		spline.terrainSmoothMultiplier = 1f;
		spline.distSmooth = 10f;
	}
}
