using System;
using System.Collections.Generic;
using Helpers;
using JetBrains.Annotations;
using Model;
using UnityEngine;

namespace Track.Search;

public static class RouteSearch
{
	public struct Metrics
	{
		public int Iterations;

		public float Distance;

		public Metrics(int iterations, float distance)
		{
			Iterations = iterations;
			Distance = distance;
		}
	}

	[Flags]
	public enum StepFlag
	{
		None = 0,
		EnterCTCSwitch = 1,
		SearchLimit = 2
	}

	public readonly struct Step
	{
		public readonly Location Location;

		[CanBeNull]
		public readonly TrackNode Node;

		public readonly StepDirection Direction;

		public readonly float Distance;

		public readonly StepFlag Flags;

		private readonly Graph _graph;

		public Vector3 Position
		{
			get
			{
				if (!(Node != null))
				{
					return _graph.GetPosition(Location);
				}
				return Node.transform.GamePosition();
			}
		}

		public Step(Location location, StepDirection direction, float distance, Graph graph, StepFlag flags = StepFlag.None)
		{
			Location = location;
			Node = null;
			Direction = direction;
			Distance = distance;
			_graph = graph;
			Flags = flags;
		}

		public Step(Location location, [NotNull] TrackNode node, StepDirection direction, float distance, Graph graph, StepFlag flags = StepFlag.None)
		{
			Location = location;
			Node = node;
			Direction = direction;
			Distance = distance;
			_graph = graph;
			Flags = flags;
		}

		public override string ToString()
		{
			string text = ((Node == null) ? "" : ("[" + Node.id + "] "));
			string text2 = (Flags.HasFlag(StepFlag.EnterCTCSwitch) ? " [EC]" : "") + (Flags.HasFlag(StepFlag.SearchLimit) ? " [SL]" : "");
			return text + Location.NodeString + " " + DirectionString() + text2;
		}

		private string DirectionString()
		{
			if (Direction != StepDirection.Out)
			{
				return "B";
			}
			return "O";
		}

		public bool Equals(Step other)
		{
			if (Node != other.Node)
			{
				return false;
			}
			if (Searcher.QuantizedLocationsEqual(Location, other.Location))
			{
				return Direction == other.Direction;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is Step other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Searcher.QuantizedLocation(Location), Node, Direction);
		}

		public Step WithLocation(Location newLocation, float newDistance)
		{
			return new Step(newLocation, Direction, newDistance, _graph, Flags);
		}

		public bool HasFlag(StepFlag flag)
		{
			return (Flags & flag) != 0;
		}
	}

	public static List<Step> FindRoute(this Graph graph, Location start, Location end, HeuristicCosts heuristicCosts, bool checkForCars = false, float trainLength = 0f, float trainMomentum = 0f, int maxIterations = 5000, HashSet<Car> checkForCarsIgnored = null, bool enableLogging = false)
	{
		List<Step> list = new List<Step>();
		graph.FindRoute(start, end, heuristicCosts, list, out var _, checkForCars, trainLength, trainMomentum, maxIterations, checkForCarsIgnored, null, null, enableLogging);
		return list;
	}

	public static bool FindRoute(this Graph graph, Location start, Location end, HeuristicCosts heuristicCosts, List<Step> routeStepsOut, out Metrics metrics, bool checkForCars = false, float trainLength = 0f, float trainMomentum = 0f, int maxIterations = 5000, HashSet<Car> checkForCarsIgnored = null, HashSet<Car> checkForCarsImpasse = null, HashSet<string> limitSwitchIds = null, bool enableLogging = false)
	{
		metrics = default(Metrics);
		bool flag = new Searcher(graph, start, end, heuristicCosts, checkForCars, trainLength, trainMomentum, checkForCarsIgnored, checkForCarsImpasse, limitSwitchIds, enableLogging).Search(routeStepsOut, maxIterations, out metrics.Iterations);
		if (flag && routeStepsOut != null)
		{
			for (int i = 0; i < routeStepsOut.Count; i++)
			{
				metrics.Distance += routeStepsOut[i].Distance;
			}
		}
		return flag;
	}
}
