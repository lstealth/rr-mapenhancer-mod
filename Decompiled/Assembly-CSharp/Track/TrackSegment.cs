using System;
using Core;
using Core.Bezier;
using Helpers;
using JetBrains.Annotations;
using Model;
using UnityEngine;

namespace Track;

public class TrackSegment : MonoBehaviour
{
	public enum Style
	{
		Standard,
		Bridge,
		Tunnel,
		Yard
	}

	public enum End
	{
		A,
		B
	}

	public string id;

	public TrackNode a;

	public TrackNode b;

	[Tooltip("Influences diverging route selection for switches, as well as RouteSearch cost.")]
	[Range(-2f, 2f)]
	public int priority;

	[Tooltip("Speed limit in miles per hour. 0 for default.")]
	[Range(0f, 45f)]
	public int speedLimit;

	public string groupId;

	public Style style;

	public TrackClass trackClass;

	[CanBeNull]
	public Turntable turntable;

	private BezierCurve? _curve;

	private BezierDistanceParameterCache _posRotCache;

	private Bounds? _boundingBox;

	private LinePoint[] _gizmosApproximation;

	public bool Available { get; set; } = true;

	public bool GroupEnabled { get; set; } = true;

	public bool IsInvisible => turntable != null;

	public BezierCurve Curve
	{
		get
		{
			if (!_curve.HasValue)
			{
				RebuildBezier();
			}
			return _curve.Value;
		}
	}

	public TrackNode GetOtherNode(TrackNode node)
	{
		if (!(node == a))
		{
			return a;
		}
		return b;
	}

	private void Awake()
	{
		if (!string.IsNullOrEmpty(id))
		{
			IdGenerator.TrackSegments.Add(id);
		}
	}

	private void OnDestroy()
	{
		_posRotCache?.Dispose();
		_posRotCache = null;
	}

	private void OnDrawGizmos()
	{
		DrawGizmos(isSelected: false);
	}

	private void DrawVelocityGizmos()
	{
	}

	private void DrawGizmos(bool isSelected)
	{
	}

	public int GetExpectedSpeedLimit()
	{
		if (speedLimit != 0)
		{
			return speedLimit;
		}
		return trackClass switch
		{
			TrackClass.Mainline => 35, 
			TrackClass.Branch => 25, 
			TrackClass.Industrial => 15, 
			_ => 35, 
		};
	}

	private static void DrawApproximationPoints(BezierCurve curve)
	{
	}

	private static float ApproximationFlatnessForDistance(float distance)
	{
		if (distance < 100f)
		{
			return 1.000001f;
		}
		if (distance < 500f)
		{
			return 1.00001f;
		}
		if (distance < 1500f)
		{
			return 1.0001f;
		}
		return 1.001f;
	}

	private static void DrawGizmoHandleLabelGrade(Vector3 handlePos, float percent)
	{
	}

	internal static void DrawGizmoText(Vector3 position, string text, Color color, float yOffset = 0.5f)
	{
	}

	private void DrawGizmoDebugCheckForCar()
	{
		TrainController shared = TrainController.Shared;
		Graph graph = shared.graph;
		for (float num = 0.5f; num < GetLength(); num += 0.5f)
		{
			Location location = new Location(this, num, End.A);
			Car car = shared.CheckForCarAtLocation(location);
			Vector3 vector = WorldTransformer.GameToWorld(graph.GetPosition(location));
			Gizmos.color = ((car == null) ? Color.green : Color.red);
			Gizmos.DrawRay(vector, Vector3.up);
		}
	}

	public void InvalidateCurve()
	{
		_curve = null;
		_boundingBox = null;
		_posRotCache?.Dispose();
		_posRotCache = null;
	}

	public void RebuildBezier()
	{
		_curve = CreateBezier();
		_gizmosApproximation = null;
	}

	public BezierCurve CreateBezier()
	{
		Vector3 localPosition = a.transform.localPosition;
		Vector3 localPosition2 = b.transform.localPosition;
		float d = (localPosition - localPosition2).magnitude * BezierTangentFactorForTangents(a.transform.forward, b.transform.forward);
		Vector3 vector = a.TangentPointAlongSegment(this, d);
		Vector3 vector2 = b.TangentPointAlongSegment(this, d);
		return new BezierCurve(new Vector3[4] { localPosition, vector, vector2, localPosition2 }, a.transform.up, b.transform.up);
	}

	public static float BezierTangentFactorForTangents(Vector3 a, Vector3 b)
	{
		float num = Vector3.Angle(a, b);
		if (num > 90f)
		{
			num = 180f - num;
		}
		return Mathf.Lerp(0.35f, 0.41f, Mathf.InverseLerp(45f, 90f, num));
	}

	public float GetLength()
	{
		PreparePosRotCacheIfNeeded();
		return _posRotCache.GetTotalLength();
	}

	public Location? LocationFromPoint(Vector3 point, float radius)
	{
		if (!BoundingBoxContains(point, radius))
		{
			return null;
		}
		Location value = new Location(this, 0f, End.A);
		float length = GetLength();
		float d = radius / 2f;
		while (value.DistanceTo(a) < length)
		{
			if ((value.GetPosition() - point).magnitude < radius)
			{
				return value;
			}
			value = value.Moving(d);
		}
		return null;
	}

	public bool BoundingBoxContains(Vector3 point, float radius)
	{
		Bounds valueOrDefault = _boundingBox.GetValueOrDefault();
		if (!_boundingBox.HasValue)
		{
			valueOrDefault = Curve.GetBounds();
			_boundingBox = valueOrDefault;
		}
		Bounds value = _boundingBox.Value;
		value.Expand(radius);
		return value.Contains(point);
	}

	public override string ToString()
	{
		return $"{base.name} id={id}, ({a})-({b})";
	}

	public bool Contains(TrackNode node)
	{
		if (!(a == node))
		{
			return b == node;
		}
		return true;
	}

	public TrackNode NodeForEnd(End end)
	{
		return end switch
		{
			End.A => a, 
			End.B => b, 
			_ => throw new ArgumentOutOfRangeException("end", end, null), 
		};
	}

	public End EndForNode(TrackNode node)
	{
		if (!(a == node))
		{
			return End.B;
		}
		return End.A;
	}

	public float DistanceBetween(TrackNode node, Location location)
	{
		End end = EndForNode(node);
		if (location.end == end)
		{
			return location.distance;
		}
		return GetLength() - location.distance;
	}

	public void GetPositionRotationAtDistance(float distance, End end, PositionAccuracy positionAccuracy, out Vector3 position, out Quaternion rotation)
	{
		PreparePosRotCacheIfNeeded();
		if (end == End.B)
		{
			distance = _posRotCache.GetTotalLength() - distance;
		}
		BezierDistanceParameterCache.Accuracy accuracy = positionAccuracy switch
		{
			PositionAccuracy.Standard => BezierDistanceParameterCache.Accuracy.Standard, 
			PositionAccuracy.High => BezierDistanceParameterCache.Accuracy.High, 
			_ => throw new ArgumentOutOfRangeException("positionAccuracy", positionAccuracy, null), 
		};
		_posRotCache.GetPositionRotationAtDistance(distance, accuracy, out var position2, out var rotation2);
		position = position2;
		rotation = rotation2;
		position += Curve.P0;
		if (end == End.B)
		{
			rotation *= Quaternion.Euler(0f, 180f, 0f);
		}
	}

	private void PreparePosRotCacheIfNeeded()
	{
		if (_posRotCache == null)
		{
			_posRotCache = new BezierDistanceParameterCache();
			CubicBezierCurve cubicBezierCurve = new CubicBezierCurve(Curve);
			CubicBezierCurve newCurve = cubicBezierCurve.Offset(-cubicBezierCurve.P0);
			_posRotCache.UpdateCache(in newCurve);
		}
	}
}
