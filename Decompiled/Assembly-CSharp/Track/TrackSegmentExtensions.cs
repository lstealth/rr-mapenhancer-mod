namespace Track;

public static class TrackSegmentExtensions
{
	public static TrackSegment.End Flipped(this TrackSegment.End end)
	{
		if (end != TrackSegment.End.A)
		{
			return TrackSegment.End.A;
		}
		return TrackSegment.End.B;
	}
}
