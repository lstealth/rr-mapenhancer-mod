using System;

namespace Track;

[Serializable]
public struct SerializableLocation
{
	public string segmentId;

	public float distance;

	public TrackSegment.End end;

	public bool IsValid => !string.IsNullOrEmpty(segmentId);

	public SerializableLocation(string segmentId, float distance, TrackSegment.End end)
	{
		this.segmentId = segmentId;
		this.distance = distance;
		this.end = end;
	}

	public SerializableLocation(Location loc)
	{
		segmentId = loc.segment.id;
		distance = loc.distance;
		end = loc.end;
	}
}
