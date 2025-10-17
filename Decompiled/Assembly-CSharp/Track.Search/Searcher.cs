using System;
using System.Collections.Generic;
using System.Diagnostics;
using Core;
using JetBrains.Annotations;
using Model;
using UnityEngine;
using UnityEngine.Pool;

namespace Track.Search;

internal class Searcher
{
	private class GCostEqualityComparer : EqualityComparer<SearchState>
	{
		public override bool Equals(SearchState x, SearchState y)
		{
			if (x.Step.Node != y.Step.Node)
			{
				return false;
			}
			return QuantizedLocationsEqual(x.Step.Location, y.Step.Location);
		}

		public override int GetHashCode(SearchState obj)
		{
			RouteSearch.Step step = obj.Step;
			(string, int, TrackSegment.End) tuple = QuantizedLocation(step.Location);
			return HashCode.Combine(step.Node, tuple.Item1, tuple.Item2, tuple.Item3);
		}
	}

	private readonly Graph _graph;

	private readonly Location _origin;

	private readonly Location _destination;

	private readonly bool _checkForCars;

	private readonly HashSet<Car> _checkForCarsIgnored;

	private readonly HashSet<Car> _checkForCarsImpasse;

	private readonly HashSet<string> _limitSwitchIds;

	private readonly float _trainLength;

	private readonly float _trainMomentum;

	private readonly bool _mustClearSwitches;

	private readonly bool _enableLogging;

	private Dictionary<object, Cost> _closedList;

	private readonly HeuristicCosts _costs;

	private readonly HashSet<ClearSwitch> _clearSwitchesScratch0 = new HashSet<ClearSwitch>();

	private readonly IEnumerable<ClearSwitch> _clearSwitchesEmpty = Array.Empty<ClearSwitch>();

	public Searcher(Graph graph, Location origin, Location destination, HeuristicCosts heuristicCosts, bool checkForCars, float trainLength, float trainMomentum, HashSet<Car> checkForCarsIgnored, HashSet<Car> checkForCarsImpasse, HashSet<string> limitSwitchIds, bool enableLogging)
	{
		origin.AssertValid();
		destination.AssertValid();
		_graph = graph;
		_origin = origin;
		_destination = destination;
		_costs = heuristicCosts;
		_checkForCars = checkForCars;
		_trainLength = trainLength;
		_trainMomentum = trainMomentum;
		_checkForCarsIgnored = checkForCarsIgnored;
		_checkForCarsImpasse = checkForCarsImpasse;
		_limitSwitchIds = limitSwitchIds;
		_mustClearSwitches = _trainLength > 0.1f;
		_enableLogging = enableLogging;
	}

	public bool Search(List<RouteSearch.Step> stepsOut, int maxIterations, out int iterationCount)
	{
		List<SearchState> obj = new List<SearchState>
		{
			new SearchState(_origin, StepDirection.Out, 0f, 0f, _graph, float.PositiveInfinity)
		};
		Location location = ((_trainLength > 0.1f) ? _graph.LocationByMoving(_origin, 0f - _trainLength, checkSwitchAgainstMovement: false, stopAtEndOfTrack: true).Flipped() : _origin.Flipped());
		obj.Add(new SearchState(location, StepDirection.Out, 0f, _trainMomentum, _graph, float.PositiveInfinity));
		List<SearchState> list = new List<SearchState>();
		bool flag = AStar<SearchState>.Search(obj, maxIterations, CalculateHeuristicCost, GetNeighbors, IsGoal, list, new GCostEqualityComparer(), out iterationCount);
		if (stepsOut != null)
		{
			stepsOut.Clear();
			if (flag)
			{
				foreach (SearchState item in list)
				{
					stepsOut.Add(item.Step);
				}
			}
		}
		return flag;
	}

	internal static (string id, int tenths, TrackSegment.End end) QuantizedLocation(Location loc)
	{
		return (id: loc.segment.id, tenths: Mathf.RoundToInt(loc.distance * 10f), end: loc.end);
	}

	internal static bool QuantizedLocationsEqual(Location x, Location y)
	{
		(string, int, TrackSegment.End) tuple = QuantizedLocation(x);
		(string, int, TrackSegment.End) tuple2 = QuantizedLocation(y);
		if (tuple.Item1 == tuple2.Item1 && tuple.Item2 == tuple2.Item2)
		{
			return tuple.Item3 == tuple2.Item3;
		}
		return false;
	}

	private bool IsGoal(SearchState p)
	{
		if (p.SearchLimit < 0f)
		{
			return true;
		}
		return EqualsDirectionless(p.Step.Location, _destination);
	}

	[Conditional("LOG_ROUTE_SEARCH")]
	private void LogRoute(string message)
	{
		if (_enableLogging)
		{
			UnityEngine.Debug.Log(message);
		}
	}

	private void GetCostToTraverseSegment(TrackSegment segment, bool isDivergingRoute, bool isThrown, bool isCTCLockedSwitch, out float segmentCost, out float branchCost)
	{
		segmentCost = segment.GetLength() * (1f + (float)(-segment.priority) / 5f);
		branchCost = 0f;
		if (isDivergingRoute)
		{
			branchCost += _costs.DivergingRoute;
		}
		if (isDivergingRoute != isThrown)
		{
			int num = (isCTCLockedSwitch ? _costs.ThrowSwitchCTCLocked : _costs.ThrowSwitch);
			branchCost += num;
		}
	}

	private void GetNeighbors(AStar<SearchState>.GetNeighborsContext ctx)
	{
		SearchState position = ctx.Position;
		RouteSearch.Step step = position.Step;
		TrackNode thisNode;
		TrackNode lastNode;
		TrackSegment lastSegment;
		SearchState priorPosition;
		bool isSwitch;
		TrackSegment enterSegment;
		TrackSegment normalSegment;
		TrackSegment divergingSegment;
		if (step.Node != null)
		{
			thisNode = step.Node;
			lastNode = null;
			lastSegment = null;
			if (ctx.TryGetPositionBefore(ctx.Position, out priorPosition))
			{
				lastSegment = step.Location.segment;
			}
			isSwitch = _graph.DecodeSwitchAt(thisNode, out enterSegment, out normalSegment, out divergingSegment);
			if (isSwitch)
			{
				if (_mustClearSwitches)
				{
					if (lastSegment == enterSegment)
					{
						AddNodeFromSegmentHelper(normalSegment);
						AddNodeFromSegmentHelper(divergingSegment);
					}
					else
					{
						AddNodeFromSegmentHelper(enterSegment);
					}
				}
				else
				{
					AddNodeFromSegmentHelper(enterSegment);
					AddNodeFromSegmentHelper(normalSegment);
					AddNodeFromSegmentHelper(divergingSegment);
				}
			}
			else
			{
				IReadOnlyList<TrackSegment> readOnlyList = _graph.SegmentsConnectedTo(thisNode);
				if (readOnlyList.Count == 2)
				{
					List<TrackSegment> list = CollectionPool<List<TrackSegment>, TrackSegment>.Get();
					list.AddRange(readOnlyList);
					TrackSegment segment = ((list[0] == lastSegment) ? list[1] : list[0]);
					CollectionPool<List<TrackSegment>, TrackSegment>.Release(list);
					AddNodeFromSegmentHelper(segment);
				}
			}
		}
		else
		{
			Location location = step.Location;
			if (location.segment == _destination.segment && DestinationIsInDirectionOfSearch(location))
			{
				float distanceBetweenClose = _graph.GetDistanceBetweenClose(location, _destination);
				Location location2 = _destination.WithEnd(location.end);
				AddLocationToOpenList(ctx, location2, step.Direction, distanceBetweenClose, distanceBetweenClose, _clearSwitchesEmpty);
			}
			if (_mustClearSwitches && !ctx.TryGetPositionBefore(position, out priorPosition))
			{
				AddNeighboursUnderTrain(ctx);
				return;
			}
			TrackNode trackNode = (location.EndIsA ? location.segment.b : location.segment.a);
			Location location3 = new Location(location.segment, location.segment.GetLength(), location.end);
			float num = location3.distance - location.distance;
			RouteSearch.StepFlag flags = FlagsForNode(trackNode, location.segment);
			AddNodeToOpenList(ctx, location3, trackNode, step.Direction, num, num, 0f, _clearSwitchesEmpty, flags);
		}
		void AddNodeFromSegmentHelper(TrackSegment segment2)
		{
			_clearSwitchesScratch0.Clear();
			AddNodeFromSegment(ctx, step.Direction, lastNode, thisNode, segment2, isSwitch, enterSegment, normalSegment, divergingSegment, lastSegment, _clearSwitchesScratch0);
		}
	}

	private void AddNodeFromSegment(AStar<SearchState>.GetNeighborsContext ctx, StepDirection direction, [CanBeNull] TrackNode lastNode, TrackNode thisNode, TrackSegment segment, bool isSwitch, [CanBeNull] TrackSegment enterSegment, [CanBeNull] TrackSegment normalSegment, [CanBeNull] TrackSegment divergingSegment, [CanBeNull] TrackSegment lastSegment, HashSet<ClearSwitch> clearSwitches)
	{
		if ((lastNode != null && segment.Contains(lastNode)) || (lastSegment != null && segment == lastSegment))
		{
			return;
		}
		bool flag = isSwitch && (normalSegment == lastSegment || divergingSegment == lastSegment);
		bool flag2 = isSwitch && (normalSegment == segment || divergingSegment == segment);
		bool flag3 = isSwitch && segment == enterSegment;
		if (!(_mustClearSwitches && flag && flag2))
		{
			bool isDivergingRoute = isSwitch && divergingSegment == segment;
			bool isCTCLockedSwitch = isSwitch && thisNode.IsCTCSwitch && !thisNode.IsCTCSwitchUnlocked;
			GetCostToTraverseSegment(segment, isDivergingRoute, thisNode.isThrown, isCTCLockedSwitch, out var segmentCost, out var branchCost);
			TrackNode otherNode = segment.GetOtherNode(thisNode);
			if (_mustClearSwitches && flag && flag3)
			{
				ClearSwitch item = new ClearSwitch(thisNode, _trainLength);
				clearSwitches.Add(item);
			}
			if (segment == _destination.segment && DestinationIsInDirectionOfSearch(new Location(segment, 0f, segment.EndForNode(thisNode))))
			{
				float num = segment.DistanceBetween(thisNode, _destination);
				Location location = _destination.WithEnd(segment.EndForNode(thisNode));
				AddLocationToOpenList(ctx, location, direction, num, num, clearSwitches);
			}
			else
			{
				float length = segment.GetLength();
				Location location2 = new Location(segment, segment.GetLength(), segment.EndForNode(thisNode));
				RouteSearch.StepFlag flags = FlagsForNode(otherNode, segment);
				AddNodeToOpenList(ctx, location2, otherNode, direction, length, segmentCost, branchCost, clearSwitches, flags);
			}
		}
	}

	private RouteSearch.StepFlag FlagsForNode(TrackNode trackNode, TrackSegment segment)
	{
		RouteSearch.StepFlag stepFlag = RouteSearch.StepFlag.None;
		if (!_graph.DecodeSwitchAt(trackNode, out var enter, out var _, out var _))
		{
			return stepFlag;
		}
		if (segment != enter)
		{
			return stepFlag;
		}
		if (trackNode.IsCTCSwitch && !trackNode.IsCTCSwitchUnlocked)
		{
			stepFlag |= RouteSearch.StepFlag.EnterCTCSwitch;
		}
		if (_limitSwitchIds != null && _limitSwitchIds.Contains(trackNode.id))
		{
			stepFlag |= RouteSearch.StepFlag.SearchLimit;
		}
		return stepFlag;
	}

	private bool DestinationIsInDirectionOfSearch(Location location)
	{
		if (!_checkForCars)
		{
			return true;
		}
		return _destination.WithEnd(location.end).distance > location.distance;
	}

	private void AddNeighboursUnderTrain(AStar<SearchState>.GetNeighborsContext ctx)
	{
		RouteSearch.Step step = ctx.Position.Step;
		Location start = step.Location;
		TrackSegment segment = start.segment;
		TrackNode trackNode = start.segment.NodeForEnd(start.end.Flipped());
		Location location = new Location(segment, 0f, segment.EndForNode(trackNode)).Flipped();
		_clearSwitchesScratch0.Clear();
		HashSet<ClearSwitch> clearSwitchesScratch = _clearSwitchesScratch0;
		float num;
		for (num = _trainLength; num > 0f; num -= 0.001f)
		{
			TrackNode node = start.segment.NodeForEnd(start.end);
			float distance = start.Distance;
			num -= distance;
			if (num < 0f)
			{
				break;
			}
			if (_graph.DecodeSwitchAt(node, out var enter, out var _, out var _) && enter == start.segment)
			{
				ClearSwitch item = new ClearSwitch(node, num);
				clearSwitchesScratch.Add(item);
			}
			start = _graph.LocationByMoving(start, 0f - (distance + 0.001f), checkSwitchAgainstMovement: false, Graph.EndOfTrackHandling.Clamp);
		}
		float distanceBetweenClose = _graph.GetDistanceBetweenClose(step.Location, location);
		RouteSearch.StepFlag flags = FlagsForNode(trackNode, segment);
		AddNodeToOpenList(ctx, location, trackNode, step.Direction, distanceBetweenClose, distanceBetweenClose, 0f, clearSwitchesScratch, flags);
	}

	private void RecycleNode(SearchState node)
	{
		CollectionPool<List<ClearSwitch>, ClearSwitch>.Release(node.ClearSwitches);
	}

	private void AddNodeToOpenList(AStar<SearchState>.GetNeighborsContext ctx, Location location, TrackNode trackNode, StepDirection direction, float distance, float segmentCost, float branchCost, IEnumerable<ClearSwitch> addClearSwitches, RouteSearch.StepFlag flags = RouteSearch.StepFlag.None)
	{
		if (_checkForCars)
		{
			if (CheckForCars(ctx.Position.Step.Location, location, out var clearUntil, out var extraCost))
			{
				location = clearUntil;
				trackNode = null;
				if (location.Equals(ctx.Position.Step.Location))
				{
					return;
				}
			}
			segmentCost += extraCost;
		}
		RouteSearch.Step step = ((trackNode == null) ? new RouteSearch.Step(location, direction, distance, _graph, flags) : new RouteSearch.Step(location, trackNode, direction, distance, _graph, flags));
		AddStepToOpenList(ctx, step, distance, segmentCost, branchCost, addClearSwitches);
	}

	private void AddLocationToOpenList(AStar<SearchState>.GetNeighborsContext ctx, Location location, StepDirection direction, float distance, float cost, IEnumerable<ClearSwitch> addClearSwitches = null)
	{
		if (_checkForCars)
		{
			if (CheckForCars(ctx.Position.Step.Location, location, out var clearUntil, out var extraCost))
			{
				location = clearUntil;
				if (location.Equals(ctx.Position.Step.Location))
				{
					return;
				}
			}
			cost += extraCost;
		}
		AddStepToOpenList(ctx, new RouteSearch.Step(location, direction, distance, _graph), distance, cost, 0f, addClearSwitches);
	}

	private bool CheckForCars(Location start, Location end, out Location clearUntil, out float extraCost)
	{
		if (_costs.CarBlockingRoute == 0)
		{
			clearUntil = end;
			extraCost = 0f;
			return false;
		}
		if (Location.TryMatchSegment(start, end.segment, out var matched))
		{
			start = matched;
		}
		bool result = false;
		float distanceBetweenClose = _graph.GetDistanceBetweenClose(start, end);
		if (distanceBetweenClose < 2f)
		{
			Location loc = _graph.LocationByMoving(start, distanceBetweenClose / 2f);
			if (EnemyCarAt(loc, distanceBetweenClose / 2f, out var foundCar))
			{
				if (_checkForCarsImpasse.Contains(foundCar))
				{
					extraCost = 0f;
					clearUntil = start;
					result = true;
				}
				else
				{
					extraCost = _costs.CarBlockingRoute;
					clearUntil = end;
				}
			}
			else
			{
				extraCost = 0f;
				clearUntil = end;
			}
			return result;
		}
		HashSet<string> hashSet = CollectionPool<HashSet<string>, string>.Get();
		clearUntil = end;
		Location location = _graph.LocationByMoving(start, 1f);
		float num = distanceBetweenClose - 2f;
		int num2 = 1 + Mathf.Max(1, Mathf.FloorToInt(num / 5f));
		float num3 = num / (float)(num2 - 1);
		Location location2 = start;
		extraCost = 0f;
		for (int i = 0; i < num2; i++)
		{
			if (EnemyCarAt(location, 1f, out var foundCar2) && hashSet.Add(foundCar2.id))
			{
				if (_checkForCarsImpasse.Contains(foundCar2))
				{
					clearUntil = location2;
					result = true;
					break;
				}
				extraCost += _costs.CarBlockingRoute;
			}
			location2 = location;
			if (i < num2 - 1)
			{
				location = _graph.LocationByMoving(location, num3, checkSwitchAgainstMovement: false, stopAtEndOfTrack: true);
				num -= num3;
			}
		}
		CollectionPool<HashSet<string>, string>.Release(hashSet);
		return result;
	}

	private bool EnemyCarAt(Location loc, float r, out Car foundCar)
	{
		Car car = TrainController.Shared.CheckForCarAtLocation(loc, r);
		bool flag = car != null && !_checkForCarsIgnored.Contains(car);
		foundCar = (flag ? car : null);
		return flag;
	}

	private static List<T> ListPoolCopy<T>(List<T> original)
	{
		List<T> list = CollectionPool<List<T>, T>.Get();
		list.AddRange(original);
		return list;
	}

	private void AddStepToOpenList(AStar<SearchState>.GetNeighborsContext ctx, RouteSearch.Step step, float stepDistance, float segmentCost, float branchCost, IEnumerable<ClearSwitch> addClearSwitches = null)
	{
		List<ClearSwitch> list = ListPoolCopy(ctx.Position.ClearSwitches);
		if (addClearSwitches != null)
		{
			foreach (ClearSwitch addClearSwitch in addClearSwitches)
			{
				bool flag = false;
				for (int i = 0; i < list.Count; i++)
				{
					ClearSwitch clearSwitch = list[i];
					if (!(addClearSwitch.ClearDistance > clearSwitch.ClearDistance))
					{
						list.Insert(i, addClearSwitch);
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					list.Add(addClearSwitch);
				}
			}
		}
		bool flag2 = step.Node != null;
		for (int num = list.Count - 1; num >= 0 && flag2; num--)
		{
			ClearSwitch value = list[num];
			if (value.ClearDistance > stepDistance)
			{
				value.ClearDistance -= stepDistance;
				list[num] = value;
				continue;
			}
			RouteSearch.Step step2 = ctx.Position.Step;
			try
			{
				TrackSegment a;
				TrackSegment a2;
				TrackSegment b;
				Location location = ((!(step2.Node != null) || !_graph.DecodeSwitchAt(step2.Node, out a, out a2, out b) || (!a2.Contains(step.Node) && !b.Contains(step.Node))) ? _graph.LocationByMoving(step2.Location, value.ClearDistance) : ((!a2.Contains(step.Node)) ? new Location(b, value.ClearDistance, b.EndForNode(step2.Node)) : new Location(a2, value.ClearDistance, a2.EndForNode(step2.Node))));
				float extraCost = 0f;
				if (_checkForCars && CheckForCars(step2.Location, location, out var _, out extraCost))
				{
					list.RemoveAt(num);
					continue;
				}
				List<AStar<SearchState>.Neighbor> list2 = ctx.AddMultistepNeighbor();
				list2.Add(new AStar<SearchState>.Neighbor(new SearchState(location, step.Direction, value.ClearDistance, 0f, _graph, ctx.Position.SearchLimit - value.ClearDistance), value.ClearDistance + extraCost));
				StepDirection direction = FlipDirection(step.Direction);
				float searchLimit = ctx.Position.SearchLimit;
				_graph.DecodeSwitchAt(value.Node, out var enter, out a, out var _);
				Location location2 = new Location(enter, enter.GetLength(), enter.EndForNode(value.Node).Flipped());
				list2.Add(new AStar<SearchState>.Neighbor(new SearchState(location2, value.Node, direction, 0f, RouteSearch.StepFlag.None, _graph, searchLimit), 0f));
			}
			catch (EndOfTrack)
			{
				list.RemoveAt(num);
				continue;
			}
			catch (Exception)
			{
				list.RemoveAt(num);
				continue;
			}
			list.RemoveAt(num);
		}
		float searchLimit2 = (step.HasFlag(RouteSearch.StepFlag.SearchLimit) ? 2000f : (ctx.Position.SearchLimit - stepDistance));
		ctx.AddNeighbor(new AStar<SearchState>.Neighbor(new SearchState(step, list, searchLimit2), branchCost + segmentCost));
	}

	private static StepDirection FlipDirection(StepDirection direction)
	{
		if (direction != StepDirection.Back)
		{
			return StepDirection.Back;
		}
		return StepDirection.Out;
	}

	private float CalculateHeuristicCost(Vector3 stepPosition)
	{
		return (stepPosition - _graph.GetPosition(_destination)).magnitude;
	}

	private float CalculateHeuristicCost(SearchState state)
	{
		return CalculateHeuristicCost(state.Step.Position) + state.OneTimeCost;
	}

	private static bool EqualsDirectionless(Location a, Location b)
	{
		if (a.segment != b.segment)
		{
			return false;
		}
		if (a.end != b.end)
		{
			b = b.Flipped();
		}
		return QuantizedLocationsEqual(a, b);
	}
}
