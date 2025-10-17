using Core;

namespace Track;

public struct SegmentProxy
{
	public readonly TrackSegment Segment;

	public BezierCurve Curve;

	public SegmentProxy(TrackSegment segment)
	{
		Segment = segment;
		Curve = segment.Curve;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is SegmentProxy segmentProxy))
		{
			return false;
		}
		if (Segment == segmentProxy.Segment)
		{
			return Curve.Equals(segmentProxy.Curve);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Segment.GetHashCode();
	}

	public SegmentProxy WithCurve(BezierCurve curve)
	{
		SegmentProxy result = new SegmentProxy(Segment);
		result.Curve = curve;
		return result;
	}
}
