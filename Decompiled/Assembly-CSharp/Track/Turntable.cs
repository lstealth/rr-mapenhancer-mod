using System;
using System.Collections.Generic;
using Core;
using Model;
using Serilog;
using UnityEngine;

namespace Track;

public class Turntable : MonoBehaviour
{
	public string id;

	[Range(10f, 20f)]
	public float radius = 27f;

	public int subdivisions = 10;

	[SerializeField]
	private List<TrackNode> nodes;

	[SerializeField]
	private int defaultStopIndex;

	[Tooltip("Group Id to use for the bridge segment.")]
	[SerializeField]
	private string bridgeGroupId;

	private TrackSegment _segment;

	private TrackNode _freeA;

	private TrackNode _freeB;

	private bool _needsInitialize = true;

	private Graph _graph;

	public float Angle { get; private set; }

	public bool IsLined => StopIndex.HasValue;

	public int? StopIndex { get; private set; }

	private Graph Graph
	{
		get
		{
			if (_graph == null)
			{
				_graph = GetComponentInParent<Graph>();
			}
			return _graph;
		}
	}

	private float DegreesPerIndex => 360f / (float)subdivisions;

	private void OnEnable()
	{
		InitializeIfNeeded();
	}

	internal void InitializeIfNeeded()
	{
		if (_needsInitialize)
		{
			_needsInitialize = false;
			Angle = AngleForIndex(defaultStopIndex);
			UpdateSegmentIndex(isMoving: false);
		}
	}

	public void SetAngle(float newAngle)
	{
		Angle = newAngle;
		_needsInitialize = false;
		Quaternion localRotation = base.transform.localRotation;
		Quaternion quaternion = Quaternion.Euler(0f, Angle, 0f);
		Quaternion quaternion2 = localRotation * quaternion;
		_freeA.transform.localPosition = base.transform.localPosition + quaternion2 * Vector3.forward * radius;
		_freeB.transform.localPosition = base.transform.localPosition + quaternion2 * Vector3.back * radius;
		_freeA.transform.localRotation = quaternion2;
		_freeB.transform.localRotation = quaternion2;
		_segment.InvalidateCurve();
	}

	private void OnValidate()
	{
		float num = radius * 2f * MathF.PI;
		subdivisions = Mathf.Clamp(subdivisions, 6, Mathf.FloorToInt(num / 1.6f));
		if (subdivisions % 2 == 1)
		{
			subdivisions--;
		}
	}

	private void OnDrawGizmosSelected()
	{
		Vector3 position = base.transform.position;
		Vector3 forward = base.transform.forward;
		Gizmos.DrawRay(position, base.transform.up);
		if (subdivisions >= 2)
		{
			Gizmos.color = Color.gray;
			for (int i = 0; i < subdivisions; i++)
			{
				float y = AngleForIndex(i);
				float y2 = AngleForIndex(i + 1);
				Gizmos.DrawLine(position + Quaternion.Euler(0f, y, 0f) * forward * radius, position + Quaternion.Euler(0f, y2, 0f) * forward * radius);
			}
			Gizmos.color = (IsLined ? Color.green : Color.red);
			Quaternion quaternion = Quaternion.Euler(0f, Angle, 0f) * base.transform.rotation;
			Gizmos.DrawLine(position + quaternion * Vector3.forward * radius, position + quaternion * Vector3.back * radius);
		}
	}

	[ContextMenu("Regenerate Nodes")]
	private void RegenerateNodes()
	{
		foreach (TrackNode node in nodes)
		{
			UnityEngine.Object.DestroyImmediate(node.gameObject);
		}
		nodes.Clear();
		Vector3 position = base.transform.position;
		_ = base.transform.forward;
		for (int i = 0; i < subdivisions; i++)
		{
			float y = AngleForIndex(i);
			Quaternion quaternion = base.transform.rotation * Quaternion.Euler(0f, y, 0f);
			nodes.Add(CreateNode(i, position + quaternion * Vector3.forward * radius, quaternion));
		}
	}

	[ContextMenu("Select Nodes")]
	private void SelectNodes()
	{
	}

	private void CreateSegmentAndNodesIfNeeded()
	{
		if (!(_segment != null))
		{
			_freeA = CreateFreeNode("free-a");
			_freeB = CreateFreeNode("free-b");
			CreateSegment();
		}
	}

	private TrackNode CreateNode(int index, Vector3 position, Quaternion rotation)
	{
		GameObject obj = new GameObject($"{base.name} Node {index}", typeof(TrackNode));
		obj.transform.SetParent(Graph.transform);
		obj.transform.position = position;
		obj.transform.rotation = rotation;
		TrackNode component = obj.GetComponent<TrackNode>();
		component.id = IdGenerator.TrackNodes.Next();
		component.turntable = this;
		return component;
	}

	private void CreateSegment()
	{
		GameObject obj = new GameObject("TT Bridge", typeof(TrackSegment));
		obj.transform.SetParent(Graph.transform);
		TrackSegment component = obj.GetComponent<TrackSegment>();
		component.id = id + "-Bridge";
		component.style = TrackSegment.Style.Bridge;
		component.turntable = this;
		component.a = _freeA;
		component.b = _freeB;
		component.groupId = bridgeGroupId;
		Graph.AddSegment(component);
		_segment = component;
	}

	private TrackNode CreateFreeNode(string suffix)
	{
		GameObject obj = new GameObject("TT Node (Free)", typeof(TrackNode));
		obj.transform.SetParent(Graph.transform);
		TrackNode component = obj.GetComponent<TrackNode>();
		component.id = id + "-" + suffix;
		component.turntable = this;
		Graph.AddNode(component);
		return component;
	}

	public float AngleForIndex(int i)
	{
		return DegreesPerIndex * (float)i;
	}

	private int? IndexForAngle()
	{
		float remainder;
		int value = IndexAndRemainderForAngle(out remainder);
		if (Mathf.Abs(remainder) > 0.1f)
		{
			return null;
		}
		return value;
	}

	public int IndexAndRemainderForAngle(out float remainder)
	{
		float num = Mathf.Repeat(Angle / DegreesPerIndex, subdivisions);
		int num2 = Mathf.RoundToInt(num);
		remainder = num - (float)num2;
		return num2 % subdivisions;
	}

	public void UpdateSegmentIndex(bool isMoving)
	{
		StopIndex = (isMoving ? ((int?)null) : IndexForAngle());
		UpdateNodesForIndex();
	}

	private bool ConnectedNodes(out TrackNode a, out TrackNode b)
	{
		if (!StopIndex.HasValue)
		{
			a = null;
			b = null;
			return false;
		}
		a = _segment.a;
		b = _segment.b;
		return a != null;
	}

	public bool SegmentsReachableFrom(TrackSegment segment, TrackSegment.End end, out TrackSegment other)
	{
		if (ConnectedNodes(out var a, out var b))
		{
			TrackNode node = ((end == TrackSegment.End.A) ? a : b);
			foreach (TrackSegment item in Graph.SegmentsConnectedTo(node))
			{
				if (!(item == _segment))
				{
					other = item;
					return true;
				}
			}
			other = null;
			return false;
		}
		other = null;
		return false;
	}

	public bool NodeIsDeadEnd(TrackNode node, out Vector3 direction)
	{
		if (_segment == null)
		{
			direction = Vector3.zero;
			return true;
		}
		if (node == _freeA || node == _freeB)
		{
			direction = Vector3.zero;
			return true;
		}
		if (node == _segment.a || node == _segment.b)
		{
			direction = node.transform.rotation * Vector3.forward;
			TrackSegment other;
			return !SegmentsReachableFrom(_segment, _segment.EndForNode(node), out other);
		}
		direction = Vector3.zero;
		return true;
	}

	public Graph.PositionRotation GetPositionRotation(Location loc)
	{
		Vector3 vector = Graph.transform.InverseTransformPoint(base.transform.position);
		bool flag = loc.end == TrackSegment.End.A;
		Quaternion quaternion = base.transform.localRotation * Quaternion.Euler(0f, Angle + (float)(flag ? 180 : 0), 0f);
		return new Graph.PositionRotation(vector + quaternion * Vector3.forward * (loc.distance - radius), quaternion);
	}

	public bool TryGetCarBlockingMovement(out Car car)
	{
		TrainController shared = TrainController.Shared;
		if (shared.CarWheelBoundsOver(_segment.a, out car))
		{
			return true;
		}
		if (shared.CarWheelBoundsOver(_segment.b, out car))
		{
			return true;
		}
		if (TryGetCoupledCarAcrossGap(TrackSegment.End.A, out car))
		{
			return true;
		}
		if (TryGetCoupledCarAcrossGap(TrackSegment.End.A, out car))
		{
			return true;
		}
		car = null;
		return false;
	}

	private bool TryGetCoupledCarAcrossGap(TrackSegment.End end, out Car car)
	{
		TrainController shared = TrainController.Shared;
		Location location = BridgeLocation(end, 0f);
		car = shared.CheckForCarAtLocation(location);
		if (car == null)
		{
			return false;
		}
		Car.LogicalEnd logicalEnd = car.ClosestLogicalEndTo(location, shared.graph);
		if (car.TryGetAdjacentCar(logicalEnd, out var _))
		{
			return car[logicalEnd].IsCoupled;
		}
		return false;
	}

	public Location BridgeLocation(TrackSegment.End end, float distance)
	{
		return new Location(_segment, distance, end);
	}

	public Location? PitLocation(int index, float distance)
	{
		TrackNode node = nodes[index];
		foreach (TrackSegment item in _graph.SegmentsConnectedTo(node))
		{
			if (!(item == _segment))
			{
				return new Location(item, distance, item.EndForNode(node));
			}
		}
		return null;
	}

	private void UpdateNodesForIndex()
	{
		CreateSegmentAndNodesIfNeeded();
		if (StopIndex.HasValue)
		{
			int value = StopIndex.Value;
			Log.Debug("Turntable {id} stopped at {index}, {angle}, {angleForIndex}", id, value, Angle, AngleForIndex(value));
			_segment.a = nodes[value];
			_segment.b = nodes[(value + nodes.Count / 2) % nodes.Count];
			_segment.InvalidateCurve();
		}
		else
		{
			Log.Debug("Turntable {id} between slots: {angle}", id, Angle);
			_segment.a = _freeA;
			_segment.b = _freeB;
			_segment.InvalidateCurve();
		}
	}

	public void SetStopIndex(int? stopIndex)
	{
		StopIndex = stopIndex;
		if (stopIndex.HasValue)
		{
			Angle = AngleForIndex(stopIndex.Value);
		}
		UpdateNodesForIndex();
	}
}
