using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Track;

public class SegmentCache
{
	private struct Record
	{
		public LineCurve LineCurve;

		public Vector3 Offset;
	}

	private readonly Dictionary<string, Record> _segmentCurveCache = new Dictionary<string, Record>();

	public (LineCurve lineCurve, Vector3 offset) CachedLineCurve(TrackSegment segment)
	{
		if (!_segmentCurveCache.TryGetValue(segment.id, out var value))
		{
			BezierCurve curve = segment.Curve;
			Vector3 endPoint = curve.EndPoint1;
			BezierCurve bezierCurve = curve.OffsetBy(-endPoint);
			value = new Record
			{
				Offset = endPoint,
				LineCurve = new LineCurve(bezierCurve.Approximate(), Hand.Left)
			};
			_segmentCurveCache[segment.id] = value;
		}
		return (lineCurve: value.LineCurve, offset: value.Offset);
	}

	public void Invalidate(string segmentId)
	{
		_segmentCurveCache.Remove(segmentId);
	}
}
