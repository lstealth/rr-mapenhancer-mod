using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Track.Search;

public static class RouteSearchPoints
{
	public static void FindPoints(this Graph graph, Location start, Location end, float step, string name, List<Vector3> output, [CanBeNull] List<TrackSegment> segmentsOut = null)
	{
		output.Clear();
		double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
		List<RouteSearch.Step> list = graph.FindRoute(start, end, HeuristicCosts.Zero, checkForCars: false, 0f, 0f, 200);
		double num = Time.realtimeSinceStartupAsDouble - realtimeSinceStartupAsDouble;
		if (list.Count <= 0)
		{
			Debug.LogError($"{name}: Search failed after {num:F3}s");
			return;
		}
		for (int i = 0; i < list.Count - 1; i++)
		{
			RouteSearch.Step step2 = list[i];
			RouteSearch.Step step3 = list[i + 1];
			if (step2.Node != null && step3.Node != null)
			{
				TrackSegment trackSegment = graph.SegmentCommonToNodes(step2.Node, step3.Node);
				Location loc = new Location(trackSegment, 0f, trackSegment.EndForNode(step2.Node));
				float length = trackSegment.GetLength();
				AddPoints(loc, length, step, graph, output);
				segmentsOut?.Add(trackSegment);
				continue;
			}
			if (step2.Node != null)
			{
				Location location = step3.Location;
				TrackSegment segment = location.segment;
				TrackSegment.End end2 = segment.EndForNode(step2.Node);
				Location loc2 = new Location(segment, 0f, end2);
				float distance = ((location.end == end2) ? location.distance : (segment.GetLength() - location.distance));
				AddPoints(loc2, distance, step, graph, output);
				segmentsOut?.Add(segment);
				continue;
			}
			if (step3.Node != null)
			{
				Location loc3 = step2.Location;
				TrackSegment segment2 = loc3.segment;
				TrackSegment.End end3 = segment2.EndForNode(step3.Node);
				float distance2 = ((loc3.end == end3) ? loc3.distance : (segment2.GetLength() - loc3.distance));
				if (loc3.end == end3)
				{
					loc3 = loc3.Flipped();
				}
				AddPoints(loc3, distance2, step, graph, output);
				segmentsOut?.Add(segment2);
				continue;
			}
			Location loc4 = step2.Location;
			Location location2 = step3.Location;
			if (location2.end != loc4.end)
			{
				location2 = location2.Flipped();
			}
			float distance3 = Mathf.Abs(location2.distance - loc4.distance);
			if (location2.distance < loc4.distance)
			{
				loc4 = loc4.Flipped();
			}
			AddPoints(loc4, distance3, step, graph, output);
			segmentsOut?.Add(loc4.segment);
		}
	}

	private static void AddPoints(Location loc, float distance, float step, Graph graph, List<Vector3> output)
	{
		float num = distance;
		while (true)
		{
			Vector3 position = graph.GetPosition(loc);
			if (output.Count > 0)
			{
				if (Vector3.Distance(output[output.Count - 1], position) < 0.001f)
				{
					output.RemoveAt(output.Count - 1);
				}
			}
			output.Add(position);
			float num2 = Mathf.Min(step, num);
			loc = graph.LocationByMoving(loc, num2, checkSwitchAgainstMovement: false, stopAtEndOfTrack: true);
			if (num < step)
			{
				break;
			}
			num -= num2;
		}
		output.Add(graph.GetPosition(loc));
	}
}
