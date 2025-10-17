using System.Collections.Generic;
using BezierCurveMesh;
using Core;
using UnityEngine;

namespace Track;

[SelectionBase]
[RequireComponent(typeof(TrackMarker))]
public class TrackCurveMeshBuilder : CurveMeshBuilderBase
{
	[SerializeField]
	private float width = 8f;

	private TrackMarker _marker;

	protected override bool ZeroMeshRotation => true;

	protected override List<BezierCurve> GetCurves()
	{
		if (_marker == null)
		{
			_marker = GetComponent<TrackMarker>();
		}
		if (!(_marker == null))
		{
			Location? location = _marker.Location;
			if (location.HasValue)
			{
				Location valueOrDefault = location.GetValueOrDefault();
				return GetCurvesFromLocation(valueOrDefault, width / 2f);
			}
		}
		Debug.LogError("Track marker is not set or invalid");
		return new List<BezierCurve>();
	}

	private static List<BezierCurve> GetCurvesFromLocation(Location location, float radius)
	{
		List<BezierCurve> list = new List<BezierCurve>();
		Graph shared = Graph.Shared;
		Location location2 = shared.LocationByMoving(location, 0f - radius, checkSwitchAgainstMovement: false, Graph.EndOfTrackHandling.Clamp);
		float num = radius * 2f;
		while (num > 0f)
		{
			TrackSegment segment = location2.segment;
			TrackNode otherNode = segment.GetOtherNode(segment.NodeForEnd(location2.end));
			float num2 = segment.DistanceBetween(otherNode, location2);
			BezierCurve item;
			if (location2.Distance > 0f)
			{
				float p = segment.Curve.ParameterForDistance(location2.Distance, 0.01f);
				segment.Curve.Split(p, out var l, out var r);
				item = (location2.EndIsA ? r : l.Reversed());
			}
			else
			{
				item = segment.Curve;
			}
			if (num2 > num)
			{
				float num3 = item.ParameterForDistance(num, 0.01f);
				if (!location2.EndIsA)
				{
					num3 = 1f - num3;
				}
				item.Split(num3, out var l2, out var r2);
				item = (location2.EndIsA ? l2 : r2.Reversed());
			}
			list.Add(item);
			num -= num2;
			if (num <= 0.1f)
			{
				break;
			}
			bool flag = false;
			foreach (TrackSegment item2 in shared.SegmentsConnectedTo(otherNode))
			{
				if (!(item2 == segment))
				{
					location2 = new Location(item2, 0f, item2.EndForNode(otherNode));
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				Debug.LogError("Failed to find next segment");
				break;
			}
		}
		return list;
	}
}
