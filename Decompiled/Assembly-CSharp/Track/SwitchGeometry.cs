using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using UnityEngine;

namespace Track;

public struct SwitchGeometry
{
	public struct RailLineCurves
	{
		public LineCurve left;

		public LineCurve right;

		public RailLineCurves(LineCurve left, LineCurve right)
		{
			this.left = left;
			this.right = right;
		}
	}

	public LinePoint[] frogPoints;

	public LineCurve leftStockRail;

	public LineCurve rightStockRail;

	public LineCurve aClosureRail;

	public LineCurve bClosureRail;

	public LineCurve aPointRail;

	public LineCurve bPointRail;

	public LineCurve leftGuardRail;

	public LineCurve rightGuardRail;

	public Vector3 switchHome;

	public Vector3 standRailCenter;

	public Vector3 standPosition;

	public Quaternion standRotation;

	private const float FlangewayWidth = 0.1f;

	public const float PointRailCutoff = 0.2f;

	public static SwitchGeometry Calculate(TrackNode node, SegmentProxy a, SegmentProxy b, out SegmentProxy sliceA, out SegmentProxy sliceB, out List<SegmentProxy> remainder)
	{
		remainder = new List<SegmentProxy>();
		SwitchGeometry result = new SwitchGeometry
		{
			frogPoints = new LinePoint[3],
			leftStockRail = null,
			rightStockRail = null
		};
		AlignSwitchCurves(a, b, out var origin, out var aCurve, out var bCurve);
		result.switchHome = origin;
		Gauge standard = Gauge.Standard;
		RailLineCurves railLineCurves = MakeTrackLineSegments(aCurve, standard);
		RailLineCurves railLineCurves2 = MakeTrackLineSegments(bCurve, standard);
		bool flag;
		if (Intersects(railLineCurves.left, railLineCurves2.right, 1.5f, out var intersection))
		{
			flag = true;
		}
		else
		{
			if (!Intersects(railLineCurves.right, railLineCurves2.left, 1.5f, out intersection))
			{
				throw new Exception("Switch tracks do not intersect: " + a.Segment.id + " and " + b.Segment.id);
			}
			flag = false;
		}
		float p = aCurve.ParameterClosestTo(intersection.point);
		float p2 = bCurve.ParameterClosestTo(intersection.point);
		aCurve.Split(p, out var l, out var r);
		bCurve.Split(p2, out var l2, out r);
		float num = l.CalculateLength();
		float num2 = l2.CalculateLength();
		float p3 = aCurve.ParameterForDistance(num + 1.5f, 0.01f);
		float p4 = bCurve.ParameterForDistance(num2 + 1.5f, 0.01f);
		aCurve.Split(p3, out var l3, out var r2);
		bCurve.Split(p4, out var l4, out var r3);
		sliceA = a.WithCurve(l3);
		sliceB = b.WithCurve(l4);
		remainder.Add(a.WithCurve(r2.OffsetBy(origin)));
		remainder.Add(b.WithCurve(r3.OffsetBy(origin)));
		RailLineCurves railLineCurves3 = MakeTrackLineSegments(l3, standard);
		RailLineCurves railLineCurves4 = MakeTrackLineSegments(l4, standard);
		LineCurve lineCurve;
		LineCurve lineCurve2;
		if (flag)
		{
			result.leftStockRail = railLineCurves4.left;
			result.rightStockRail = railLineCurves3.right;
			result.frogPoints[0] = railLineCurves3.left.Points.Last();
			result.frogPoints[1] = intersection;
			result.frogPoints[2] = railLineCurves4.right.Points.Last();
			lineCurve = railLineCurves.left;
			lineCurve2 = railLineCurves2.right;
		}
		else
		{
			result.leftStockRail = railLineCurves3.left;
			result.rightStockRail = railLineCurves4.right;
			result.frogPoints[0] = railLineCurves3.right.Points.Last();
			result.frogPoints[1] = intersection;
			result.frogPoints[2] = railLineCurves4.left.Points.Last();
			lineCurve = railLineCurves.right;
			lineCurve2 = railLineCurves2.left;
		}
		result.leftGuardRail = MakeGuardRail(result.leftStockRail, intersection);
		result.rightGuardRail = MakeGuardRail(result.rightStockRail, intersection);
		new Vector3(0.1f, 0f, 0f);
		float distance = lineCurve.DistanceTo(intersection.point) - 0.45f;
		float distance2 = lineCurve2.DistanceTo(intersection.point) - 0.45f;
		LineCurve lineCurve3 = lineCurve.Take(distance);
		LineCurve lineCurve4 = lineCurve2.Take(distance2);
		Quaternion rotation = result.frogPoints[2].Rotation;
		Quaternion rotation2 = result.frogPoints[0].Rotation;
		lineCurve3.Add(new LinePoint(result.frogPoints[2].point + rotation * ((lineCurve3.hand == Hand.Left) ? Vector3.left : Vector3.right) * 0.1f, rotation));
		lineCurve4.Add(new LinePoint(result.frogPoints[0].point + rotation2 * ((lineCurve4.hand == Hand.Left) ? Vector3.left : Vector3.right) * 0.1f, rotation2));
		float distance3 = Mathf.Lerp(lineCurve3.Length, lineCurve4.Length, 0.5f) / 2f;
		lineCurve3.Split(distance3, out result.aPointRail, out result.aClosureRail);
		lineCurve4.Split(distance3, out result.bPointRail, out result.bClosureRail);
		float num3 = aCurve.ParameterForDistance(0.4f, 0.1f);
		result.standRailCenter = aCurve.GetPoint(num3);
		Vector3 vector = aCurve.GetDirection(num3);
		if (node.flipSwitchStand)
		{
			vector = -vector;
		}
		result.standRotation = Quaternion.LookRotation(vector);
		result.standPosition = result.standRailCenter + result.standRotation * new Vector3(0f, 0f - Gauge.Standard.RailHeight, 0f);
		return result;
	}

	private static LineCurve MakeGuardRail(LineCurve stockRail, LinePoint frogPoint)
	{
		float offset = (float)((stockRail.hand == Hand.Left) ? 1 : (-1)) * 0.15f;
		LineCurve lineCurve = stockRail.Parallel(offset);
		for (int i = 0; i < 100; i++)
		{
			float num = Vector3.Distance(lineCurve.Head.point, frogPoint.point);
			if (num < 1.5f || Mathf.Abs(1.5f - num) < 0.0015f)
			{
				break;
			}
			lineCurve = lineCurve.Skip(num - 1.5f);
		}
		float num2 = Mathf.Tan(0.17453292f) * 0.25f;
		Vector3 vector = ((lineCurve.hand == Hand.Left) ? Vector3.right : Vector3.left);
		Quaternion quaternion = Quaternion.Euler(0f, 10f * (float)((lineCurve.hand != Hand.Left) ? 1 : (-1)), 0f);
		LinePoint head = lineCurve.Head;
		lineCurve = lineCurve.Skip(0.25f);
		lineCurve.Insert(0, new LinePoint(head.point + head.Rotation * vector * num2, quaternion * head.Rotation));
		lineCurve = lineCurve.Reverse();
		head = lineCurve.Head;
		lineCurve = lineCurve.Skip(0.25f);
		lineCurve.Insert(0, new LinePoint(head.point + head.Rotation * vector * num2, quaternion * head.Rotation));
		return lineCurve.Reverse();
	}

	private static void AlignSwitchCurves(SegmentProxy a, SegmentProxy b, out Vector3 origin, out BezierCurve aCurve, out BezierCurve bCurve)
	{
		if (a.Curve.EndPoint1 == b.Curve.EndPoint1)
		{
			aCurve = a.Curve;
			bCurve = b.Curve;
		}
		else if (a.Curve.EndPoint1 == b.Curve.EndPoint2)
		{
			aCurve = a.Curve;
			bCurve = b.Curve.Reversed();
		}
		else if (a.Curve.EndPoint2 == b.Curve.EndPoint2)
		{
			aCurve = a.Curve.Reversed();
			bCurve = b.Curve.Reversed();
		}
		else
		{
			if (!(a.Curve.EndPoint2 == b.Curve.EndPoint1))
			{
				throw new Exception("a " + a.Segment.id + " and b " + b.Segment.id + " don't share common endpoint");
			}
			aCurve = a.Curve.Reversed();
			bCurve = b.Curve;
		}
		origin = aCurve.EndPoint1;
		aCurve = aCurve.OffsetBy(-origin);
		bCurve = bCurve.OffsetBy(-origin);
	}

	public static RailLineCurves MakeTrackLineSegments(BezierCurve center, Gauge gauge)
	{
		LineCurve lineCurve = new LineCurve(center.Approximate(), Hand.Left);
		LineCurve left = lineCurve.Parallel((0f - gauge.Inside) / 2f, Hand.Left);
		LineCurve right = lineCurve.Parallel(gauge.Inside / 2f, Hand.Right);
		return new RailLineCurves(left, right);
	}

	private static bool Intersects(LineCurve aCurve, LineCurve bCurve, float frogDepth, out LinePoint intersection)
	{
		aCurve.Points.ToList();
		bCurve.Points.ToList();
		foreach (var segment in aCurve.Segments)
		{
			foreach (var segment2 in bCurve.Segments)
			{
				if (LineSegment.Intersects(segment.Item2, segment2.Item2, out var point, 0.02f))
				{
					LineSegment item = segment.Item2;
					if (item.Length == 0f)
					{
						intersection = item.a;
						return true;
					}
					intersection = LinePoint.Lerp(item.a, item.b, (item.a.point - point).magnitude / item.Length);
					return true;
				}
			}
		}
		intersection = default(LinePoint);
		return false;
	}
}
