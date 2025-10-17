using System.Collections.Generic;
using System.Linq;
using Serilog;
using UnityEngine;

namespace Track.Search;

public static class GraphRouteSearchExtension
{
	public static List<TrackSegment> FindRoute(this Graph graph, Location start, Location end)
	{
		double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
		List<RouteSearch.Step> list = graph.FindRoute(start, end, HeuristicCosts.Zero);
		double realtimeSinceStartupAsDouble2 = Time.realtimeSinceStartupAsDouble;
		if (list.Count <= 0)
		{
			Debug.LogWarning($"Search failed after {realtimeSinceStartupAsDouble2 - realtimeSinceStartupAsDouble:F3}s");
			return new List<TrackSegment>();
		}
		Debug.Log(string.Format("Search result {0} after {1:F3}s:\n{2}", list.Count, realtimeSinceStartupAsDouble2 - realtimeSinceStartupAsDouble, string.Join("\n", list)));
		List<TrackSegment> list2 = new List<TrackSegment>();
		for (int i = 0; i < list.Count; i++)
		{
			if (i == 0)
			{
				list2.Add(list[0].Location.segment);
				continue;
			}
			RouteSearch.Step step = list[i - 1];
			RouteSearch.Step step2 = list[i];
			TrackNode node = ((step.Node != null) ? step.Node : step.Location.segment.NodeForEnd(step.Location.end));
			TrackNode node2 = ((step2.Node != null) ? step2.Node : step2.Location.segment.NodeForEnd(step2.Location.end));
			TrackSegment item = graph.SegmentsConnectedTo(node).Intersect(graph.SegmentsConnectedTo(node2)).First();
			if (!list2.Contains(item))
			{
				list2.Add(item);
			}
		}
		return list2;
	}

	public static List<TrackSegment> FindRoute(this Graph graph, TrackNode start, TrackNode end)
	{
		TrackSegment trackSegment = (from s in graph.SegmentsConnectedTo(start)
			orderby Vector3.Distance(s.Curve.GetPoint(0.5f), end.transform.position)
			select s).First();
		TrackSegment trackSegment2 = (from s in graph.SegmentsConnectedTo(end)
			orderby Vector3.Distance(s.Curve.GetPoint(0.5f), start.transform.position)
			select s).First();
		Location start2 = new Location(trackSegment, 0f, (!(trackSegment.a == start)) ? TrackSegment.End.B : TrackSegment.End.A);
		Location end2 = new Location(trackSegment2, 0f, (!(trackSegment2.a == end)) ? TrackSegment.End.B : TrackSegment.End.A);
		return graph.FindRoute(start2, end2);
	}

	public static bool TryFindDistance(this Graph graph, Location start, Location end, out float totalDistance, out float traverseTimeSeconds)
	{
		traverseTimeSeconds = 0f;
		List<RouteSearch.Step> list = graph.FindRoute(start, end, HeuristicCosts.Zero);
		if (list.Count == 0)
		{
			Log.Error("No route between {start} and {end}", start, end);
			totalDistance = 0f;
			return false;
		}
		float num = 0f;
		for (int i = 0; i < list.Count - 1; i++)
		{
			RouteSearch.Step step = list[i];
			RouteSearch.Step step2 = list[i + 1];
			if (step.Node != null && step2.Node != null)
			{
				TrackSegment trackSegment = graph.SegmentCommonToNodes(step.Node, step2.Node);
				float length = trackSegment.GetLength();
				num += length;
				traverseTimeSeconds += EstimateSecondsToTraverse(trackSegment, length);
				continue;
			}
			if (step.Node != null)
			{
				Location location = step2.Location;
				TrackSegment segment = location.segment;
				TrackSegment.End end2 = segment.EndForNode(step.Node);
				float num2 = ((location.end == end2) ? location.distance : (segment.GetLength() - location.distance));
				num += num2;
				traverseTimeSeconds += EstimateSecondsToTraverse(segment, num2);
				continue;
			}
			if (step2.Node != null)
			{
				Location location2 = step.Location;
				TrackSegment segment2 = location2.segment;
				TrackSegment.End end3 = segment2.EndForNode(step2.Node);
				float num3 = ((location2.end == end3) ? location2.distance : (segment2.GetLength() - location2.distance));
				num += num3;
				traverseTimeSeconds += EstimateSecondsToTraverse(segment2, num3);
				continue;
			}
			Location location3 = step.Location;
			Location location4 = step2.Location;
			if (location4.end != location3.end)
			{
				location4 = location4.Flipped();
			}
			float num4 = Mathf.Abs(location4.distance - location3.distance);
			num += num4;
			traverseTimeSeconds += EstimateSecondsToTraverse(location3.segment, num4);
		}
		totalDistance = num;
		return true;
		static float EstimateSecondsToTraverse(TrackSegment trackSegment2, float distance)
		{
			if (distance == 0f)
			{
				return 0f;
			}
			int expectedSpeedLimit = trackSegment2.GetExpectedSpeedLimit();
			return distance * 0.0006213712f / (float)expectedSpeedLimit * 60f * 60f;
		}
	}
}
