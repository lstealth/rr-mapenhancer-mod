using System;
using System.Linq;
using Model;

namespace Track;

public static class TrainControllerExtensions
{
	public static bool FindOpenSpaceFromLower(this TrainController trainController, TrackSpan span, float lengthInMeters, Func<Car, bool> canCoupleToCar, out Location location, out Car car)
	{
		Location value = span.lower.Value;
		Graph graph = trainController.graph;
		car = trainController.CarsOnSpan(span).FirstOrDefault();
		if (car != null)
		{
			float distanceBetweenClose = graph.GetDistanceBetweenClose(value, car.LocationA);
			float distanceBetweenClose2 = graph.GetDistanceBetweenClose(value, car.LocationB);
			float num;
			Location location2;
			if (!(distanceBetweenClose < distanceBetweenClose2))
			{
				Location locationB = car.LocationB;
				num = distanceBetweenClose2;
				location2 = locationB;
			}
			else
			{
				Location locationB = car.LocationA;
				num = distanceBetweenClose;
				location2 = locationB;
			}
			location = location2;
			if (num < lengthInMeters || !canCoupleToCar(car))
			{
				car = null;
				return false;
			}
		}
		else
		{
			location = graph.LocationByMoving(span.upper.Value, 1f);
			car = null;
		}
		location = graph.LocationOrientedToward(location, value);
		return true;
	}
}
