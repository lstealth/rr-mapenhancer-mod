using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Reputation;

public class PassengerReputationCalculator
{
	public class Stop
	{
		public readonly string Id;

		public readonly List<Stop> Neighbors = new List<Stop>();

		public Stop(string id)
		{
			Id = id;
		}
	}

	public static float Calculate(IEnumerable<Stop> passengerStopsIn, HashSet<string> playerVisitedEdges)
	{
		List<Stop> passengerStops = passengerStopsIn.OrderBy((Stop ps) => ps.Neighbors.Count).ToList();
		int count = passengerStops.Count;
		HashSet<string> countedEdges = new HashSet<string>();
		float num = 0f;
		while (passengerStops.Count > 0)
		{
			Stop passengerStop = passengerStops[0];
			HashSet<string> hashSet = new HashSet<string>();
			SearchFrom(passengerStop, hashSet);
			num += Mathf.Pow((float)hashSet.Count / (float)count, 2f);
		}
		return Mathf.Pow(num, 0.5f);
		void SearchFrom(Stop stop, HashSet<string> hitStops)
		{
			if (!passengerStops.Contains(stop))
			{
				return;
			}
			passengerStops.Remove(stop);
			foreach (Stop item2 in stop.Neighbors.Where(passengerStops.Contains).ToList())
			{
				string item = ReputationTracker.KeyForPassengerStopEdge(stop.Id, item2.Id);
				if (playerVisitedEdges.Contains(item) && !countedEdges.Contains(item))
				{
					countedEdges.Add(item);
					hitStops.Add(stop.Id);
					hitStops.Add(item2.Id);
					SearchFrom(item2, hitStops);
				}
			}
		}
	}
}
