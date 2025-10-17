using System;
using System.Collections.Generic;
using Core;
using Helpers;
using Serilog;
using Track.Search;
using UnityEngine;

namespace Track;

public class TrackSpan : MonoBehaviour
{
	public string id;

	private static readonly IReadOnlyCollection<Vector3> EmptyPoints = new List<Vector3>();

	[SerializeField]
	[HideInInspector]
	private SerializableLocation _lower;

	[SerializeField]
	[HideInInspector]
	private SerializableLocation _upper;

	private Graph _graph;

	private readonly List<Vector3> _cachedPoints = new List<Vector3>();

	private readonly List<TrackSegment> _cachedSegments = new List<TrackSegment>();

	private float _length;

	private bool _warnedInvalid;

	public Location? lower
	{
		get
		{
			if (!_lower.IsValid)
			{
				return null;
			}
			return Graph.MakeLocation(_lower);
		}
		set
		{
			_lower = value?.Serializable() ?? default(SerializableLocation);
			InvalidateCache();
		}
	}

	public Location? upper
	{
		get
		{
			if (!_upper.IsValid)
			{
				return null;
			}
			return Graph.MakeLocation(_upper);
		}
		set
		{
			_upper = value?.Serializable() ?? default(SerializableLocation);
			InvalidateCache();
		}
	}

	public float Length
	{
		get
		{
			UpdateCachedPointsIfNeeded();
			return _length;
		}
	}

	private Graph Graph
	{
		get
		{
			if (_graph != null)
			{
				return _graph;
			}
			_graph = UnityEngine.Object.FindObjectOfType<Graph>();
			if (_graph == null)
			{
				Debug.LogError("Couldn't find Graph");
			}
			return _graph;
		}
	}

	public static Color ColorEdit => Color.red;

	public static Color ColorLower => Color.cyan;

	public static Color ColorUpper => Color.yellow;

	public bool IsValid
	{
		get
		{
			Location? location = lower;
			Location? location2 = upper;
			if (location.HasValue && location2.HasValue && location.Value.IsValid)
			{
				return location2.Value.IsValid;
			}
			return false;
		}
	}

	private void OnValidate()
	{
		if (!Application.isPlaying && !(lower?.segment == null) && !(upper?.segment == null))
		{
			NormalizeUpperLower();
		}
	}

	private void OnDrawGizmos()
	{
		DrawGizmos(Color.magenta * 0.7f);
	}

	private void OnDrawGizmosSelected()
	{
		UpdateCachedPointsIfNeeded();
		if (_cachedPoints.Count != 0)
		{
			Gizmos.matrix = Matrix4x4.Translate(Vector3.zero.GameToWorld());
			Gizmos.color = Color.magenta;
			DrawGizmoLines(Vector3.up * 0.5f);
		}
	}

	private void DrawGizmosEndpoints()
	{
	}

	private void DrawGizmoLines(Vector3 offset)
	{
		UpdateCachedPointsIfNeeded();
		for (int i = 0; i < _cachedPoints.Count - 1; i++)
		{
			Vector3 vector = _cachedPoints[i];
			Gizmos.DrawLine(to: _cachedPoints[i + 1] + offset, from: vector + offset);
		}
	}

	[ContextMenu("Normalize Upper-Lower")]
	public void NormalizeUpperLower()
	{
		UpdateCachedPointsIfNeeded();
		if (_cachedSegments.Count == 0)
		{
			return;
		}
		if (_cachedSegments.Count == 1)
		{
			if (!lower.Value.EndIsA)
			{
				lower = lower.Value.Flipped();
			}
			if (!upper.Value.EndIsA)
			{
				upper = upper.Value.Flipped();
			}
			if (upper.Value.distance > lower.Value.distance)
			{
				upper = upper.Value.Flipped();
			}
			else
			{
				lower = lower.Value.Flipped();
			}
			return;
		}
		Location value = lower.Value;
		Location value2 = upper.Value;
		TrackSegment trackSegment = _cachedSegments[1];
		if (value.EndIsA && trackSegment.Contains(value.segment.a))
		{
			value = value.Flipped();
		}
		else if (!value.EndIsA && trackSegment.Contains(value.segment.b))
		{
			value = value.Flipped();
		}
		List<TrackSegment> cachedSegments = _cachedSegments;
		trackSegment = cachedSegments[cachedSegments.Count - 2];
		if (value2.EndIsA && trackSegment.Contains(value2.segment.a))
		{
			value2 = value2.Flipped();
		}
		else if (!value2.EndIsA && trackSegment.Contains(value2.segment.b))
		{
			value2 = value2.Flipped();
		}
		lower = value;
		upper = value2;
	}

	public bool Contains(Location loc)
	{
		UpdateCachedPointsIfNeeded();
		for (int i = 0; i < _cachedSegments.Count; i++)
		{
			if (_cachedSegments[i] != loc.segment)
			{
				continue;
			}
			if (0 < i && i < _cachedSegments.Count - 1)
			{
				return true;
			}
			if (i == 0)
			{
				if (loc.end == lower.Value.end && loc.distance < lower.Value.distance)
				{
					return false;
				}
				if (loc.end != lower.Value.end && loc.Flipped().distance < lower.Value.distance)
				{
					return false;
				}
			}
			if (i == _cachedSegments.Count - 1)
			{
				if (loc.end == upper.Value.end && loc.distance < upper.Value.distance)
				{
					return false;
				}
				if (loc.end != upper.Value.end && loc.Flipped().distance < upper.Value.distance)
				{
					return false;
				}
			}
			return true;
		}
		return false;
	}

	public bool Contains(Vector3 point, float radius)
	{
		UpdateCachedPointsIfNeeded();
		if (_cachedPoints.Count == 0)
		{
			return false;
		}
		LineSegment b = new LineSegment(new LinePoint(point, Quaternion.identity), new LinePoint(point + Vector3.up, Quaternion.identity));
		for (int i = 0; i < _cachedPoints.Count - 1; i++)
		{
			Vector3 point2 = _cachedPoints[i];
			Vector3 point3 = _cachedPoints[i + 1];
			if (LineSegment.Intersects(new LineSegment(new LinePoint(point2, Quaternion.identity), new LinePoint(point3, Quaternion.identity)), b, out var _, radius))
			{
				return true;
			}
		}
		return false;
	}

	public IReadOnlyCollection<Vector3> GetPoints()
	{
		UpdateCachedPointsIfNeeded();
		IReadOnlyCollection<Vector3> cachedPoints = _cachedPoints;
		return cachedPoints ?? EmptyPoints;
	}

	public IReadOnlyCollection<TrackSegment> GetSegments()
	{
		UpdateCachedPointsIfNeeded();
		return _cachedSegments;
	}

	public Vector3 GetCenterPoint()
	{
		UpdateCachedPointsIfNeeded();
		if (_cachedPoints.Count == 0)
		{
			Log.Warning("TrackSpan {id} has no points, returning transform", id);
			return base.transform.position.WorldToGame();
		}
		return _cachedPoints[_cachedPoints.Count / 2];
	}

	public void InvalidateCache()
	{
		_cachedPoints.Clear();
		_cachedSegments.Clear();
		_length = 0f;
	}

	private void UpdateCachedPointsIfNeeded(bool warnInvalid = true)
	{
		if (_cachedPoints.Count > 0)
		{
			return;
		}
		Location? location = lower;
		Location? location2 = upper;
		if (!location.HasValue || !location2.HasValue || location.Value.segment == null || location2.Value.segment == null)
		{
			if (!(_graph == null) && _graph.HasPopulatedCollections && !_warnedInvalid && warnInvalid)
			{
				Log.Warning("Span {name} {id} missing points or invalid", base.name, id);
				_warnedInvalid = true;
			}
			return;
		}
		_warnedInvalid = false;
		Graph graph = Graph;
		try
		{
			graph.FindPoints(location.Value.Clamped(), location2.Value.Clamped(), 10f, base.name, _cachedPoints, _cachedSegments);
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Error finding points of {spanId} from {a} to {b}", id, location.Value.NodeString, location2.Value.NodeString);
			return;
		}
		finally
		{
		}
		CalculateLengthFromCachedPoints();
		if (_cachedPoints.Count == 0)
		{
			Log.Error("TrackSpan {name} has no route", base.name);
		}
	}

	private void CalculateLengthFromCachedPoints()
	{
		_length = 0f;
		for (int i = 0; i < _cachedPoints.Count - 1; i++)
		{
			Vector3 a = _cachedPoints[i];
			Vector3 b = _cachedPoints[i + 1];
			_length += Vector3.Distance(a, b);
		}
	}

	public Mesh BuildMesh(Matrix4x4 matrix)
	{
		UpdateCachedPointsIfNeeded();
		Vector3[] array = _cachedPoints.ToArray();
		Quaternion[] array2 = new Quaternion[array.Length];
		for (int i = 0; i < array.Length - 1; i++)
		{
			array2[i] = Quaternion.LookRotation(array[i + 1] - array[i], Vector3.up);
		}
		array2[^1] = array2[^2];
		return TrackMeshBuilder.BuildColliderMesh(array, array2, Gauge.Standard);
	}

	public void DrawGizmos(Color color, float scale = 1f)
	{
		if (Vector3.SqrMagnitude(base.transform.position - Camera.current.transform.position) > 4000000f)
		{
			return;
		}
		UpdateCachedPointsIfNeeded(warnInvalid: false);
		if (_cachedPoints.Count != 0)
		{
			Gizmos.color = color;
			Matrix4x4 matrix = Gizmos.matrix;
			for (int i = 2; i < _cachedPoints.Count; i += 2)
			{
				Vector3 vector = _cachedPoints[i - 1];
				Vector3 vector2 = _cachedPoints[i];
				Quaternion q = ((vector2 == vector) ? Quaternion.identity : Quaternion.LookRotation(vector2 - vector));
				Gizmos.matrix = Matrix4x4.TRS(vector + Vector3.up * 0.5f, q, Vector3.one) * matrix;
				Gizmos.DrawCube(Vector3.zero, new Vector3(0.2f, 0.2f, 2f) * scale);
			}
			Gizmos.matrix = matrix;
		}
	}

	public void SwapUpperLower()
	{
		Location? location = lower;
		Location? location2 = upper;
		Location? location3 = (upper = location);
		location3 = (lower = location2);
		InvalidateCache();
		NormalizeUpperLower();
	}
}
