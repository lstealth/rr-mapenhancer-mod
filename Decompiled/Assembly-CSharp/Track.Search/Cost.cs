using System;

namespace Track.Search;

internal readonly struct Cost : IComparable<Cost>
{
	public readonly float distanceTraveled;

	private readonly float heuristicCost;

	private readonly float totalCost;

	public Cost(float distanceTraveled, float heuristicCost)
	{
		this.distanceTraveled = distanceTraveled;
		this.heuristicCost = heuristicCost;
		totalCost = distanceTraveled + heuristicCost;
	}

	public int CompareTo(Cost other)
	{
		float num = totalCost;
		return num.CompareTo(other.totalCost);
	}

	public override string ToString()
	{
		return $"{totalCost:N1} = {distanceTraveled:N1} + {heuristicCost:n1}";
	}
}
