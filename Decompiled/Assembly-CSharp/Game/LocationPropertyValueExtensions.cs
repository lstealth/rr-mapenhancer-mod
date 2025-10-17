using System.Collections.Generic;
using KeyValue.Runtime;
using Track;

namespace Game;

public static class LocationPropertyValueExtensions
{
	public static Value PropertyValue(this Location location)
	{
		return Value.Dictionary(new Dictionary<string, Value>
		{
			{
				"seg",
				Value.String(location.segment.id)
			},
			{
				"dist",
				Value.Float(location.distance)
			},
			{
				"end",
				Value.String(location.EndIsA ? "a" : "b")
			}
		});
	}

	public static Location LocationFrom(this Graph graph, Value value)
	{
		string stringValue = value["seg"].StringValue;
		float floatValue = value["dist"].FloatValue;
		TrackSegment.End end = ((!(value["end"].StringValue == "a")) ? TrackSegment.End.B : TrackSegment.End.A);
		return new Location(graph.GetSegment(stringValue), floatValue, end);
	}
}
