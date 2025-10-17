using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Pool;

namespace Track.Search;

internal readonly struct SearchState : IEquatable<SearchState>
{
	public readonly RouteSearch.Step Step;

	public readonly List<ClearSwitch> ClearSwitches;

	public readonly float SearchLimit;

	public readonly float OneTimeCost;

	public SearchState(Location location, StepDirection direction, float distance, float oneTimeCost, Graph graph, float searchLimit)
	{
		Step = new RouteSearch.Step(location, direction, distance, graph);
		ClearSwitches = CollectionPool<List<ClearSwitch>, ClearSwitch>.Get();
		SearchLimit = searchLimit;
		OneTimeCost = oneTimeCost;
	}

	public SearchState(Location location, TrackNode node, StepDirection direction, float distance, RouteSearch.StepFlag flags, Graph graph, float searchLimit)
	{
		Step = new RouteSearch.Step(location, node, direction, distance, graph, flags);
		ClearSwitches = CollectionPool<List<ClearSwitch>, ClearSwitch>.Get();
		SearchLimit = searchLimit;
		OneTimeCost = 0f;
	}

	public SearchState(RouteSearch.Step step, List<ClearSwitch> clearSwitches, float searchLimit)
	{
		Step = step;
		ClearSwitches = clearSwitches;
		SearchLimit = searchLimit;
		OneTimeCost = 0f;
	}

	public override string ToString()
	{
		return string.Format("Step = {0}, ClearSwitches = [{1}]", Step, string.Join(", ", ClearSwitches));
	}

	public bool Equals(SearchState other)
	{
		if (Step.Equals(other.Step))
		{
			return ClearSwitches.SequenceEqual(other.ClearSwitches);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is SearchState other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Step, ClearSwitches);
	}
}
