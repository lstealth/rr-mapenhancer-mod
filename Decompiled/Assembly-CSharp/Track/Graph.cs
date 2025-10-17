using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Core;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.Messages;
using Serilog;
using UnityEngine;

namespace Track;

[DefaultExecutionOrder(-1)]
public class Graph : MonoBehaviour
{
	public enum EndOfTrackHandling
	{
		Throw,
		Clamp,
		Unclamped
	}

	private struct DecodedSwitchInfo
	{
		public readonly TrackSegment Enter;

		public readonly TrackSegment A;

		public readonly TrackSegment B;

		public readonly bool Found;

		public DecodedSwitchInfo(TrackSegment enter, TrackSegment a, TrackSegment b, bool found)
		{
			Enter = enter;
			A = a;
			B = b;
			Found = found;
		}
	}

	public struct PositionRotation
	{
		public Vector3 Position;

		public Quaternion Rotation;

		public PositionRotation(Vector3 position, Quaternion rotation)
		{
			Position = position;
			Rotation = rotation;
		}

		public override string ToString()
		{
			return $"({Position:F1}, {Rotation.eulerAngles:F1})";
		}

		public PositionRotation Project(float distance)
		{
			if (distance == 0f)
			{
				return this;
			}
			return new PositionRotation(Position + Rotation * (Vector3.forward * distance), Rotation);
		}
	}

	public struct PositionDirection
	{
		public Vector3 Position;

		public Vector3 Direction;

		public PositionDirection(Vector3 position, Vector3 direction)
		{
			Position = position;
			Direction = direction;
		}
	}

	public enum CurveQueryResolution
	{
		Interpolate,
		Segment
	}

	public class BadLocationException : Exception
	{
		public BadLocationException(string message)
			: base(message)
		{
		}
	}

	private static Graph _graph;

	public List<string> groupIds = new List<string>();

	[Tooltip("Enabled groups are visible track.")]
	public List<string> enabledGroupIds = new List<string>();

	[Tooltip("Available groups may be picked.")]
	public List<string> availableGroupIds = new List<string>();

	private readonly Dictionary<string, TrackNode> nodes = new Dictionary<string, TrackNode>();

	private readonly Dictionary<string, TrackSegment> segments = new Dictionary<string, TrackSegment>();

	private readonly Dictionary<string, TrackSpan> spans = new Dictionary<string, TrackSpan>();

	private readonly SegmentCache _segmentCurveCache = new SegmentCache();

	private readonly Dictionary<string, List<TrackSegment>> _nodeConnectionsCache = new Dictionary<string, List<TrackSegment>>();

	private readonly Dictionary<string, Vector3?> _nodeIsDeadEndCache = new Dictionary<string, Vector3?>();

	private readonly Dictionary<(TrackSegment, TrackSegment.End), (TrackSegment, TrackSegment)> _cachedReachableSegments = new Dictionary<(TrackSegment, TrackSegment.End), (TrackSegment, TrackSegment)>();

	private readonly Dictionary<string, DecodedSwitchInfo> _decodedSwitchCache = new Dictionary<string, DecodedSwitchInfo>();

	private readonly List<TrackSegment> _segmentsReachableFromOthers = new List<TrackSegment>();

	private HashSet<TurntableController> _cachedTurntableControllers;

	private readonly Dictionary<string, byte[]> _curvatureSampleCache = new Dictionary<string, byte[]>();

	private const int SampleCurveUncachedSamples = 5;

	private static readonly byte[] SampleCurveUncachedArray = new byte[5];

	private static readonly PositionRotation[] SampleCurveUncachedPosRot = new PositionRotation[6];

	private readonly Dictionary<string, HashSet<TrackMarker>> _trackMarkers = new Dictionary<string, HashSet<TrackMarker>>();

	private readonly HashSet<TrackMarker> _pendingTrackMarkers = new HashSet<TrackMarker>();

	public static Graph Shared
	{
		get
		{
			if (_graph == null)
			{
				_graph = UnityEngine.Object.FindObjectOfType<Graph>();
			}
			return _graph;
		}
	}

	public IEnumerable<TrackNode> Nodes => nodes.Values;

	public IEnumerable<TrackSegment> Segments => segments.Values;

	public bool HasPopulatedCollections { get; private set; }

	public IEnumerable<TurntableController> TurntableControllers
	{
		get
		{
			if (_cachedTurntableControllers == null)
			{
				_cachedTurntableControllers = new HashSet<TurntableController>();
				TurntableController[] array = UnityEngine.Object.FindObjectsOfType<TurntableController>();
				foreach (TurntableController item in array)
				{
					_cachedTurntableControllers.Add(item);
				}
			}
			return _cachedTurntableControllers;
		}
	}

	public event Action<TrackNode> NodeDidChange;

	private void Awake()
	{
		RebuildCollections();
	}

	public void RebuildCollections()
	{
		Log.Debug("RebuildCollections");
		nodes.Clear();
		segments.Clear();
		spans.Clear();
		_nodeConnectionsCache.Clear();
		_nodeIsDeadEndCache.Clear();
		_cachedReachableSegments.Clear();
		_decodedSwitchCache.Clear();
		TrackNode[] componentsInChildren = GetComponentsInChildren<TrackNode>();
		foreach (TrackNode node in componentsInChildren)
		{
			AddNode(node);
		}
		TrackSegment[] componentsInChildren2 = GetComponentsInChildren<TrackSegment>();
		foreach (TrackSegment segment in componentsInChildren2)
		{
			AddSegment(segment, invalidateNodes: false);
		}
		foreach (TrackSegment value2 in segments.Values)
		{
			AddConnection(value2.a, value2);
			AddConnection(value2.b, value2);
		}
		TrackSpan[] array = UnityEngine.Object.FindObjectsOfType<TrackSpan>();
		foreach (TrackSpan span in array)
		{
			AddSpan(span);
		}
		HasPopulatedCollections = true;
		foreach (TrackMarker pendingTrackMarker in _pendingTrackMarkers)
		{
			RegisterTrackMarker(pendingTrackMarker);
		}
		_pendingTrackMarkers.Clear();
		Messenger.Default.Send(default(GraphDidRebuildCollections));
		void AddConnection(TrackNode trackNode, TrackSegment item)
		{
			if (!(trackNode.turntable != null))
			{
				if (!_nodeConnectionsCache.TryGetValue(trackNode.id, out var value))
				{
					_nodeConnectionsCache.Add(trackNode.id, value = new List<TrackSegment>(2));
				}
				value.Add(item);
			}
		}
	}

	public void AddNode(TrackNode node)
	{
		if (nodes.ContainsKey(node.id))
		{
			Debug.LogError($"Node {node} has duplicate id: {node.id}", node);
		}
		else
		{
			nodes[node.id] = node;
		}
	}

	public void AddSegment(TrackSegment segment, bool invalidateNodes = true)
	{
		if (!string.IsNullOrEmpty(segment.groupId))
		{
			segment.GroupEnabled = enabledGroupIds.Contains(segment.groupId);
			if (!segment.GroupEnabled)
			{
				return;
			}
			segment.Available = availableGroupIds.Contains(segment.groupId);
		}
		else
		{
			segment.GroupEnabled = true;
			segment.Available = true;
		}
		if (segments.ContainsKey(segment.id))
		{
			Debug.LogError($"Segment {segment} has duplicate id: {segment.id}", segment);
			return;
		}
		segments[segment.id] = segment;
		if (invalidateNodes)
		{
			InvalidateNode(segment.a);
			InvalidateNode(segment.b);
		}
	}

	private void AddSpan(TrackSpan span)
	{
		if (spans.ContainsKey(span.id))
		{
			Debug.LogError($"Span {span} has duplicate id: {span.id}", span);
		}
		else
		{
			spans[span.id] = span;
		}
	}

	public void OnNodeDidChange(TrackNode node)
	{
		InvalidateNode(node);
		this.NodeDidChange?.Invoke(node);
	}

	public void InvalidateNode(TrackNode node)
	{
		foreach (TrackSegment item in SegmentsConnectedTo(node))
		{
			item.InvalidateCurve();
			_segmentCurveCache.Invalidate(item.id);
			_curvatureSampleCache.Remove(item.id);
		}
		_nodeIsDeadEndCache.Remove(node.id);
		_nodeConnectionsCache.Remove(node.id);
	}

	private void GraphDidChange()
	{
		_cachedReachableSegments.Clear();
		_decodedSwitchCache.Clear();
	}

	public TrackNode AddNode(string id, Vector3 position, Quaternion rotation)
	{
		TrackNode trackNode = new GameObject
		{
			name = "Node-" + id
		}.AddComponent<TrackNode>();
		trackNode.id = id;
		trackNode.transform.position = position;
		trackNode.transform.rotation = rotation;
		AddNode(trackNode);
		return trackNode;
	}

	public TrackSegment AddSegment(string id, TrackNode a, TrackNode b)
	{
		TrackSegment trackSegment = new GameObject
		{
			name = "Segment-" + id
		}.AddComponent<TrackSegment>();
		trackSegment.id = id;
		trackSegment.a = a;
		trackSegment.b = b;
		AddSegment(trackSegment);
		GraphDidChange();
		return trackSegment;
	}

	public TrackNode AddNode(Vector3 position, Quaternion rotation, string id = null)
	{
		return AddNode(id ?? IdGenerator.TrackNodes.Next(), position, rotation);
	}

	public TrackSegment AddSegment(TrackNode a, TrackNode b, string id = null)
	{
		return AddSegment(id ?? IdGenerator.TrackSegments.Next(), a, b);
	}

	public TrackNode GetNode(string id)
	{
		return nodes.GetValueOrDefault(id);
	}

	public TrackSegment GetSegment(string id)
	{
		return segments.GetValueOrDefault(id);
	}

	public TrackSpan SpanForId(string id)
	{
		return spans.GetValueOrDefault(id);
	}

	public Location LocationByMoving(Location start, float distance, bool checkSwitchAgainstMovement = false, bool stopAtEndOfTrack = false)
	{
		if (distance == 0f)
		{
			return start;
		}
		EndOfTrackHandling endOfTrackHandling = (stopAtEndOfTrack ? EndOfTrackHandling.Clamp : EndOfTrackHandling.Throw);
		return LocationByMoving(start, distance, checkSwitchAgainstMovement, endOfTrackHandling);
	}

	public Location LocationByMoving(Location start, float distance, bool checkSwitchAgainstMovement, EndOfTrackHandling endOfTrackHandling)
	{
		int num = 1000;
		Location location = new Location(start);
		bool flag = false;
		if (distance < 0f)
		{
			flag = true;
			location = location.Flipped();
			distance = 0f - distance;
		}
		float num2 = distance;
		while (num2 > 0f)
		{
			float num3 = location.DistanceUntilEnd();
			if (num2 < num3)
			{
				location = location.Moving(num2);
				num2 = 0f;
			}
			else
			{
				num2 -= num3;
				TrackSegment.End end = (location.EndIsA ? TrackSegment.End.B : TrackSegment.End.A);
				Location? location2 = LocationFrom(location.segment, end, checkSwitchAgainstMovement);
				if (!location2.HasValue)
				{
					switch (endOfTrackHandling)
					{
					case EndOfTrackHandling.Throw:
						throw new EndOfTrack();
					case EndOfTrackHandling.Clamp:
						return new Location(location.segment, location.segment.GetLength(), location.end);
					case EndOfTrackHandling.Unclamped:
						break;
					default:
						throw new ArgumentOutOfRangeException("endOfTrackHandling", endOfTrackHandling, null);
					}
					location2 = location.Moving(num3 + num2);
					num2 = 0f;
				}
				location = location2.Value;
			}
			num--;
			if (num <= 0)
			{
				throw new Exception("Maximum iterations reached at " + location);
			}
		}
		Location result = location;
		if (flag)
		{
			result = result.Flipped();
		}
		return result;
	}

	public Location? LocationFrom(TrackSegment seg, TrackSegment.End end, bool checkSwitchAgainstMovement = false)
	{
		TrackNode trackNode = ((end == TrackSegment.End.B) ? seg.b : seg.a);
		SegmentsReachableFrom(seg, end, out var normal, out var reversed);
		TrackSegment trackSegment;
		if (normal != null && reversed == null)
		{
			trackSegment = normal;
			if (checkSwitchAgainstMovement)
			{
				CheckSwitchAgainstMovement(seg, trackSegment, trackNode);
			}
		}
		else
		{
			if (!(normal != null) || !(reversed != null))
			{
				return null;
			}
			trackSegment = (trackNode.isThrown ? reversed : normal);
		}
		bool flag = trackSegment.a == trackNode;
		Location value = new Location(trackSegment, 0f, (!flag) ? TrackSegment.End.B : TrackSegment.End.A);
		if (Math.Abs(value.DistanceUntilEnd()) < 0.1f)
		{
			throw new Exception("DistanceUntilEnd is zero");
		}
		return value;
	}

	private void CheckSwitchAgainstMovement(TrackSegment seg, TrackSegment nextSegment, TrackNode node)
	{
		SegmentsReachableFrom(nextSegment, (node == nextSegment.b) ? TrackSegment.End.B : TrackSegment.End.A, out var normal, out var reversed);
		if (!(normal == null) && !(reversed == null))
		{
			bool num = !node.isThrown && normal != seg;
			bool flag = node.isThrown && reversed != seg;
			if (num || flag)
			{
				throw new SwitchAgainstMovement(node);
			}
		}
	}

	public IReadOnlyList<TrackSegment> SegmentsConnectedTo(TrackNode node)
	{
		if (_nodeConnectionsCache.TryGetValue(node.id, out var value))
		{
			return value;
		}
		value = new List<TrackSegment>();
		foreach (var (_, trackSegment2) in segments)
		{
			if (trackSegment2.a == node || trackSegment2.b == node)
			{
				value.Add(trackSegment2);
			}
		}
		if (node.turntable == null)
		{
			_nodeConnectionsCache[node.id] = value;
		}
		return value;
	}

	private void SegmentsReachableFrom(TrackSegment segment, TrackSegment.End end, out TrackSegment normal, out TrackSegment reversed)
	{
		if (segment.turntable != null)
		{
			segment.turntable.SegmentsReachableFrom(segment, end, out normal);
			reversed = null;
			return;
		}
		if (_cachedReachableSegments.TryGetValue((segment, end), out var value))
		{
			(normal, reversed) = value;
			return;
		}
		TrackNode trackNode = segment.NodeForEnd(end);
		List<TrackSegment> segmentsReachableFromOthers = _segmentsReachableFromOthers;
		segmentsReachableFromOthers.Clear();
		foreach (TrackSegment item in SegmentsConnectedTo(trackNode))
		{
			if (item != segment)
			{
				segmentsReachableFromOthers.Add(item);
			}
		}
		if (segmentsReachableFromOthers.Count > 1)
		{
			DecodeSwitchAt(trackNode, out var enter, out var a, out var b);
			if (enter == segment)
			{
				normal = a;
				reversed = b;
			}
			else
			{
				normal = enter;
				reversed = null;
			}
		}
		else
		{
			normal = ((segmentsReachableFromOthers.Count > 0) ? segmentsReachableFromOthers[0] : null);
			reversed = null;
		}
		if ((!(normal != null) || !(normal.turntable != null)) && !(trackNode.turntable != null))
		{
			_cachedReachableSegments[(segment, end)] = (normal, reversed);
		}
	}

	public bool IsSwitch(TrackNode node)
	{
		return SegmentsConnectedTo(node).Count() == 3;
	}

	public bool DecodeSwitchAt(TrackNode node, out TrackSegment enter, out TrackSegment a, out TrackSegment b)
	{
		if (_decodedSwitchCache.TryGetValue(node.id, out var value))
		{
			enter = value.Enter;
			a = value.A;
			b = value.B;
			return value.Found;
		}
		IReadOnlyList<TrackSegment> readOnlyList = SegmentsConnectedTo(node);
		if (readOnlyList.Count != 3)
		{
			enter = null;
			a = null;
			b = null;
			_decodedSwitchCache[node.id] = new DecodedSwitchInfo(enter, a, b, found: false);
			return false;
		}
		List<TrackSegment> list = readOnlyList.ToList();
		bool num = node.SegmentCanReachSegment(list[0], list[1]);
		bool flag = node.SegmentCanReachSegment(list[1], list[2]);
		bool flag2 = node.SegmentCanReachSegment(list[0], list[2]);
		if (num && flag)
		{
			enter = list[1];
			a = list[0];
			b = list[2];
		}
		else if (flag && flag2)
		{
			enter = list[2];
			a = list[0];
			b = list[1];
		}
		else
		{
			enter = list[0];
			a = list[1];
			b = list[2];
		}
		bool flag3;
		if (a.priority == b.priority)
		{
			float f = DivergingAngleOf(node, a);
			float f2 = DivergingAngleOf(node, b);
			flag3 = Mathf.Abs(f) < Mathf.Abs(f2);
		}
		else
		{
			flag3 = a.priority > b.priority;
		}
		if (!flag3)
		{
			TrackSegment trackSegment = b;
			TrackSegment trackSegment2 = a;
			a = trackSegment;
			b = trackSegment2;
		}
		_decodedSwitchCache[node.id] = new DecodedSwitchInfo(enter, a, b, found: true);
		return true;
	}

	private float DivergingAngleOf(TrackNode node, TrackSegment segment)
	{
		float length = segment.GetLength();
		float num = Mathf.Min(5f, length);
		Location loc = new Location(segment, ((object)node == segment.a) ? num : (length - num), TrackSegment.End.A);
		Vector3 position = GetPosition(loc);
		Vector3 vector = node.TangentPointAlongSegment(segment, num);
		Vector3 localPosition = node.transform.localPosition;
		return Vector3.Angle(position - localPosition, vector - localPosition);
	}

	public bool NodeIsDeadEnd(TrackNode node, out Vector3 direction)
	{
		if (node.turntable != null)
		{
			return node.turntable.NodeIsDeadEnd(node, out direction);
		}
		if (_nodeIsDeadEndCache.TryGetValue(node.id, out var value))
		{
			direction = value ?? Vector3.zero;
			return value.HasValue;
		}
		bool flag = NodeIsDeadEndUncached(node, out direction);
		_nodeIsDeadEndCache[node.id] = (flag ? new Vector3?(direction) : ((Vector3?)null));
		return flag;
	}

	private bool NodeIsDeadEndUncached(TrackNode node, out Vector3 direction)
	{
		direction = Vector3.zero;
		TrackSegment trackSegment = null;
		int num = 0;
		foreach (var (_, trackSegment3) in segments)
		{
			if (!(trackSegment3.a != node) || !(trackSegment3.b != node))
			{
				if (num == 0)
				{
					trackSegment = trackSegment3;
				}
				num++;
				if (num > 1)
				{
					return false;
				}
			}
		}
		if (num == 0)
		{
			return false;
		}
		Location location = new Location(trackSegment, 0f, (!(trackSegment.a == node)) ? TrackSegment.End.B : TrackSegment.End.A);
		direction = location.GetRotation() * Vector3.forward;
		direction.Scale(-Vector3.one);
		return true;
	}

	public bool TryGetLocationFromWorldPoint(Vector3 worldPosition, float radius, out Location output)
	{
		Vector3 gamePosition = worldPosition - base.transform.position;
		return TryGetLocationFromGamePoint(gamePosition, radius, out output);
	}

	public bool TryGetLocationFromGamePoint(Vector3 gamePosition, float radius, out Location output)
	{
		Location? location = null;
		float num = float.MaxValue;
		foreach (var (_, trackSegment2) in segments)
		{
			if (TryGetLocationFromPoint(trackSegment2, gamePosition, radius, out var output2))
			{
				float num2 = Vector3.Distance(GetPosition(output2), gamePosition);
				if (num2 < num)
				{
					num = num2;
					location = output2;
				}
			}
		}
		if (location.HasValue)
		{
			Location valueOrDefault = location.GetValueOrDefault();
			output = valueOrDefault;
			return true;
		}
		output = default(Location);
		return false;
	}

	private bool TryGetLocationFromPoint(TrackSegment trackSegment, Vector3 queryPoint, float radius, out Location output)
	{
		output = default(Location);
		if (!trackSegment.BoundingBoxContains(queryPoint, radius))
		{
			return false;
		}
		(LineCurve lineCurve, Vector3 offset) tuple = _segmentCurveCache.CachedLineCurve(trackSegment);
		LineCurve item = tuple.lineCurve;
		Vector3 item2 = tuple.offset;
		Vector3 vector = queryPoint - item2;
		(int, LineSegment, float)? tuple2 = null;
		(int, LineSegment)[] array = item.Segments.ToArray();
		(int, LineSegment)[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			(int, LineSegment) tuple3 = array2[i];
			int item3 = tuple3.Item1;
			LineSegment item4 = tuple3.Item2;
			float magnitude = (item4.ClosestPointTo(vector) - vector).magnitude;
			if (!(magnitude > radius) && (!tuple2.HasValue || tuple2.Value.Item3 > magnitude))
			{
				tuple2 = (item3, item4, magnitude);
			}
		}
		if (!tuple2.HasValue)
		{
			return false;
		}
		(int, LineSegment, float) value = tuple2.Value;
		int item5 = value.Item1;
		LineSegment item6 = value.Item2;
		Vector3 vector2 = item6.ClosestPointTo(vector);
		float num = 0f;
		for (int j = 0; j < item5; j++)
		{
			num += array[j].Item2.Length;
		}
		num += (array[item5].Item2.a.point - vector2).magnitude;
		output = new Location(trackSegment, num, TrackSegment.End.A).Clamped();
		return true;
	}

	public PositionRotation GetPositionRotation(Location loc, PositionAccuracy accuracy = PositionAccuracy.Standard)
	{
		loc = loc.Clamped(out var remainder);
		TrackSegment segment = loc.segment;
		if ((object)segment.turntable != null)
		{
			return segment.turntable.GetPositionRotation(loc).Project(remainder);
		}
		try
		{
			segment.GetPositionRotationAtDistance(loc.distance, loc.end, accuracy, out var position, out var rotation);
			return new PositionRotation(position, rotation).Project(remainder);
		}
		catch (Exception innerException)
		{
			throw new Exception($"Error from cache, loc: {loc}", innerException);
		}
		finally
		{
		}
	}

	public PositionDirection GetPositionDirection(Location loc)
	{
		PositionRotation positionRotation = GetPositionRotation(loc);
		return new PositionDirection(positionRotation.Position, positionRotation.Rotation * Vector3.forward);
	}

	public Vector3 GetPosition(Location loc)
	{
		return GetPositionRotation(loc).Position;
	}

	public HashSet<TrackSegment> SegmentsAffectedByNodes(HashSet<TrackNode> theNodes)
	{
		if (theNodes.Count == nodes.Count)
		{
			return new HashSet<TrackSegment>(segments.Values);
		}
		HashSet<TrackSegment> hashSet = new HashSet<TrackSegment>();
		foreach (TrackNode theNode in theNodes)
		{
			hashSet.UnionWith(SegmentsConnectedTo(theNode));
		}
		return hashSet;
	}

	public TrackSegment SegmentCommonToNodes(TrackNode a, TrackNode b)
	{
		if (a == null || b == null)
		{
			return null;
		}
		foreach (TrackSegment item in SegmentsConnectedTo(a))
		{
			if (item.a == b || item.b == b)
			{
				return item;
			}
		}
		return null;
	}

	public Location Lerp(Location a, Location b, float p)
	{
		if (p <= 0f)
		{
			return a;
		}
		if (p >= 1f)
		{
			return b;
		}
		if (a.segment == b.segment)
		{
			if (a.end != b.end)
			{
				b = b.Flipped();
			}
			return new Location(a.segment, Mathf.Lerp(a.Distance, b.Distance, p), a.end);
		}
		Vector3 position = GetPosition(a);
		Vector3 position2 = GetPosition(b);
		float magnitude = (position - position2).magnitude;
		float num = Mathf.Lerp(0f, magnitude, p);
		Location location = LocationByMoving(a, num);
		Vector3 position3 = GetPosition(location);
		float num2 = Vector3.Dot(position2 - position3, position - position3);
		if (!(num2 <= 0f))
		{
			float num3 = num2;
			location = LocationByMoving(a, 0f - num);
			position3 = GetPosition(location);
			num2 = Vector3.Dot(position2 - position3, position - position3);
			Log.Verbose("Lerp across segment a = {a}, b = {b}, p = {p}; dot was {oldDot}, now {dot}.", a, b, p, num3, num2);
		}
		return location;
	}

	public float GetDistanceBetweenClose(Location a, Location b)
	{
		if (a.segment == b.segment)
		{
			if (a.end == b.end)
			{
				return Mathf.Abs(a.Distance - b.Distance);
			}
			return Mathf.Abs(a.Distance - b.Flipped().Distance);
		}
		Vector3 position = GetPosition(a);
		Vector3 position2 = GetPosition(b);
		return Vector3.Distance(position, position2);
	}

	public Location MakeLocation(Snapshot.TrackLocation location)
	{
		TrackSegment segment = GetSegment(location.segmentId);
		if (segment == null)
		{
			throw new InvalidLocationException("Segment not found: " + location.segmentId);
		}
		return new Location(segment, location.distance, (!location.endIsA) ? TrackSegment.End.B : TrackSegment.End.A);
	}

	public static Snapshot.TrackLocation CreateSnapshotTrackLocation(Location loc)
	{
		return new Snapshot.TrackLocation(loc.segment.id, loc.Distance, loc.EndIsA);
	}

	public Location MakeLocation(SerializableLocation location)
	{
		return new Location(GetSegment(location.segmentId), location.distance, location.end);
	}

	public bool TryGetDistanceBetweenSameRoute(Location a, Location b, out float distance)
	{
		float distanceBetweenClose = GetDistanceBetweenClose(a, b);
		if (CheckSameRoute(a, b, distanceBetweenClose * 2f, out distance))
		{
			return true;
		}
		distance = distanceBetweenClose;
		return false;
	}

	public bool CheckSameRoute(Location from, Location to, float limit)
	{
		float actualDistance;
		return CheckSameRoute(from, to, limit, out actualDistance);
	}

	public bool CheckSameRoute(Location from, Location to, float limit, out float actualDistance)
	{
		if (CheckSameRoute(from, to, TrackSegment.End.A, limit, out actualDistance))
		{
			return true;
		}
		if (CheckSameRoute(from, to, TrackSegment.End.B, limit, out actualDistance))
		{
			return true;
		}
		return false;
	}

	private bool CheckSameRoute(Location cursor, Location target, TrackSegment.End end, float limit, out float actualDistance)
	{
		actualDistance = 0f;
		while (actualDistance <= limit)
		{
			if (cursor.segment == target.segment)
			{
				float num = Mathf.Abs(cursor.DistanceTo(end) - target.DistanceTo(end));
				actualDistance += num;
				return actualDistance <= limit;
			}
			float num2 = cursor.DistanceTo(cursor.segment.NodeForEnd(end));
			actualDistance += num2;
			if (actualDistance > limit)
			{
				return false;
			}
			try
			{
				Location? location = LocationFrom(cursor.segment, end, checkSwitchAgainstMovement: true);
				if (!location.HasValue)
				{
					return false;
				}
				cursor = location.Value;
				end = (cursor.EndIsA ? TrackSegment.End.B : TrackSegment.End.A);
			}
			catch (SwitchAgainstMovement)
			{
				return false;
			}
		}
		return false;
	}

	public static IEnumerable<(List<TrackSegment>, List<TrackNode>)> EnumerateContinuousSegments(IEnumerable<TrackSegment> segments)
	{
		List<TrackSegment> remaining = segments.ToList();
		while (remaining.Count > 0)
		{
			TrackSegment trackSegment = remaining[0];
			remaining.RemoveAt(0);
			List<TrackSegment> list = new List<TrackSegment> { trackSegment };
			List<TrackNode> list2 = new List<TrackNode> { trackSegment.a, trackSegment.b };
			TrackSegment trackSegment2 = trackSegment;
			bool flag = true;
			TrackSegment.End end = TrackSegment.End.A;
			while (flag && remaining.Count > 0)
			{
				flag = false;
				for (int num = remaining.Count - 1; num >= 0; num--)
				{
					TrackSegment trackSegment3 = remaining[num];
					TrackNode trackNode = trackSegment2.NodeForEnd(end);
					bool flag2 = trackSegment3.a == trackNode;
					bool flag3 = trackSegment3.b == trackNode;
					if (flag2 || flag3)
					{
						end = (flag2 ? TrackSegment.End.B : TrackSegment.End.A);
						list.Insert(0, trackSegment3);
						list2.Insert(0, flag2 ? trackSegment3.b : trackSegment3.a);
						remaining.RemoveAt(num);
						trackSegment2 = trackSegment3;
						flag = true;
						break;
					}
				}
			}
			trackSegment2 = trackSegment;
			flag = true;
			end = TrackSegment.End.B;
			while (flag && remaining.Count > 0)
			{
				flag = false;
				for (int num2 = remaining.Count - 1; num2 >= 0; num2--)
				{
					TrackSegment trackSegment4 = remaining[num2];
					TrackNode trackNode2 = trackSegment2.NodeForEnd(end);
					bool flag4 = trackSegment4.a == trackNode2;
					bool flag5 = trackSegment4.b == trackNode2;
					if (flag4 || flag5)
					{
						end = (flag4 ? TrackSegment.End.B : TrackSegment.End.A);
						list.Add(trackSegment4);
						list2.Add(flag4 ? trackSegment4.b : trackSegment4.a);
						remaining.RemoveAt(num2);
						trackSegment2 = trackSegment4;
						flag = true;
						break;
					}
				}
			}
			yield return (list, list2);
		}
	}

	private List<TrackSegment> SegmentsFromContinuousNodes(List<TrackNode> nodes)
	{
		List<TrackSegment> list = new List<TrackSegment>();
		HashSet<TrackSegment> hashSet = SegmentsAffectedByNodes(new HashSet<TrackNode>(nodes));
		for (int i = 0; i < nodes.Count - 1; i++)
		{
			TrackNode trackNode = nodes[i];
			TrackNode trackNode2 = nodes[i + 1];
			foreach (TrackSegment item in hashSet)
			{
				if ((item.a == trackNode || item.b == trackNode) && (item.a == trackNode2 || item.b == trackNode2))
				{
					list.Add(item);
					break;
				}
			}
		}
		return list;
	}

	public TurntableController TurntableControllerForId(string turntableId)
	{
		return TurntableControllers.FirstOrDefault((TurntableController tt) => tt.turntable.id == turntableId);
	}

	public float CurvatureAtLocation(Location location, CurveQueryResolution resolution = CurveQueryResolution.Interpolate)
	{
		TrackSegment segment = location.segment;
		byte[] curvatureSamples = GetCurvatureSamples(segment);
		if (resolution == CurveQueryResolution.Segment)
		{
			return (int)curvatureSamples.Max();
		}
		int num = Mathf.FloorToInt(location.DistanceTo(TrackSegment.End.A) / segment.GetLength() * (float)curvatureSamples.Length);
		if (num >= curvatureSamples.Length)
		{
			num = curvatureSamples.Length - 1;
		}
		return (int)curvatureSamples[num];
	}

	private byte[] GetCurvatureSamples(TrackSegment segment)
	{
		if (!_curvatureSampleCache.TryGetValue(segment.id, out var value))
		{
			value = SampleCurvature(segment);
			_curvatureSampleCache[segment.id] = value;
		}
		return value;
	}

	public byte[] SampleCurvature(TrackSegment segment)
	{
		byte[] array = new byte[5];
		PositionRotation[] array2 = new PositionRotation[6];
		float length = segment.GetLength();
		for (int i = 0; i < 6; i++)
		{
			float distance = length * ((float)i / 5f);
			array2[i] = GetPositionRotation(new Location(segment, distance, TrackSegment.End.A));
		}
		for (int j = 0; j < 5; j++)
		{
			float f = TrackMath.CalculateCurveDegrees(array2[j], array2[j + 1]);
			array[j] = (byte)Mathf.RoundToInt(f);
		}
		return array;
	}

	public static byte[] SampleCurvatureUncached(TrackSegment segment)
	{
		PositionRotation[] sampleCurveUncachedPosRot = SampleCurveUncachedPosRot;
		BezierCurve curve = segment.Curve;
		for (int i = 0; i < 6; i++)
		{
			float num = (float)i / 5f;
			PositionRotation positionRotation = new PositionRotation(curve.GetPoint(num), curve.GetRotation(num));
			sampleCurveUncachedPosRot[i] = positionRotation;
		}
		for (int j = 0; j < 5; j++)
		{
			float f = TrackMath.CalculateCurveDegrees(sampleCurveUncachedPosRot[j], sampleCurveUncachedPosRot[j + 1]);
			SampleCurveUncachedArray[j] = (byte)Mathf.RoundToInt(f);
		}
		return SampleCurveUncachedArray;
	}

	public float GradeAtLocation(Location location)
	{
		float num = GetPositionRotation(location).Rotation.eulerAngles.x;
		if (num > 180f)
		{
			num -= 360f;
		}
		return num * 1.746f;
	}

	public void RegisterTrackMarker(TrackMarker trackMarker)
	{
		if (!HasPopulatedCollections)
		{
			_pendingTrackMarkers.Add(trackMarker);
			return;
		}
		Location? location = trackMarker.Location;
		if (!location.HasValue || !location.Value.IsValid)
		{
			Debug.LogWarning("Can't register TrackMarker with null/invalid Location: " + trackMarker.name + " " + trackMarker.id, trackMarker);
			return;
		}
		string id = location.Value.segment.id;
		if (!_trackMarkers.TryGetValue(id, out var value))
		{
			_trackMarkers.Add(id, value = new HashSet<TrackMarker>());
		}
		value.Add(trackMarker);
	}

	public void UnregisterTrackMarker(TrackMarker trackMarker)
	{
		_pendingTrackMarkers.Remove(trackMarker);
		foreach (HashSet<TrackMarker> value in _trackMarkers.Values)
		{
			value.Remove(trackMarker);
		}
	}

	public TrackMarker MarkerForId(string id)
	{
		foreach (HashSet<TrackMarker> value in _trackMarkers.Values)
		{
			foreach (TrackMarker item in value)
			{
				if (item.id == id)
				{
					return item;
				}
			}
		}
		return null;
	}

	public IEnumerable<TrackMarker> EnumerateTrackMarkers(Location start, float distance, bool sameDirection)
	{
		start.AssertValid();
		Location cursor = new Location(start);
		if (distance < 0f)
		{
			cursor = cursor.Flipped();
			distance = 0f - distance;
		}
		float remaining = distance;
		foreach (TrackMarker item in EnumerateToEndOfSegment(cursor, remaining))
		{
			yield return item;
		}
		while (remaining > 0f)
		{
			float num = cursor.DistanceUntilEnd();
			if (remaining < num)
			{
				break;
			}
			remaining -= num;
			TrackSegment.End end = (cursor.EndIsA ? TrackSegment.End.B : TrackSegment.End.A);
			Location? location = LocationFrom(cursor.segment, end);
			if (!location.HasValue)
			{
				break;
			}
			cursor = location.Value;
			cursor.AssertValid();
			foreach (TrackMarker item2 in EnumerateToEndOfSegment(cursor, remaining))
			{
				yield return item2;
			}
		}
		IEnumerable<TrackMarker> EnumerateToEndOfSegment(Location location2, float distanceLimit)
		{
			if (_trackMarkers.TryGetValue(location2.segment.id, out var value))
			{
				IOrderedEnumerable<TrackMarker> orderedEnumerable = from tm in value.Where(delegate(TrackMarker tm)
					{
						Location value2 = tm.Location.Value;
						float num2 = value2.DistanceTo(location2.end) - location2.distance;
						bool flag = 0f <= num2 && num2 <= distanceLimit;
						return sameDirection ? (value2.end == location2.end && flag) : flag;
					})
					orderby tm.Location.Value.DistanceTo(location2.end)
					select tm;
				foreach (TrackMarker item3 in orderedEnumerable)
				{
					yield return item3;
				}
			}
		}
	}

	public bool SetGroupEnabled(string groupId, bool groupEnabled)
	{
		int count = enabledGroupIds.Count;
		if (groupEnabled)
		{
			enabledGroupIds = enabledGroupIds.Concat(new string[1] { groupId }).ToHashSet().ToList();
			enabledGroupIds.Sort();
		}
		else
		{
			enabledGroupIds = enabledGroupIds.Where((string id) => id != groupId).ToList();
		}
		bool num = count != enabledGroupIds.Count;
		if (num)
		{
			Messenger.Default.Send(default(GraphDidChangeEnabledGroups));
		}
		return num;
	}

	public bool SetGroupAvailable(string groupId, bool groupAvailable)
	{
		int count = availableGroupIds.Count;
		if (groupAvailable)
		{
			availableGroupIds = availableGroupIds.Concat(new string[1] { groupId }).ToHashSet().ToList();
			availableGroupIds.Sort();
		}
		else
		{
			availableGroupIds = availableGroupIds.Where((string id) => id != groupId).ToList();
		}
		bool num = count != availableGroupIds.Count;
		if (num)
		{
			Messenger.Default.Send(default(GraphDidChangeAvailableGroups));
		}
		return num;
	}

	public Location LocationOrientedToward(Location location, Location toward)
	{
		float num = GetDistanceBetweenClose(location, toward) * 0.5f;
		PositionDirection positionDirection = GetPositionDirection(location);
		Vector3 position = GetPosition(toward);
		if (Vector3.Distance(positionDirection.Position + positionDirection.Direction * num, position) < Vector3.Distance(positionDirection.Position, position))
		{
			return location.Flipped();
		}
		return location;
	}

	public float CalculateFoulingDistance(TrackNode node)
	{
		if (!DecodeSwitchAt(node, out var _, out var a, out var b))
		{
			throw new ArgumentException($"Failed to decode switch node {node}");
		}
		Location location = new Location(a, 0f, a.EndForNode(node));
		Location location2 = new Location(b, 0f, b.EndForNode(node));
		float num = 0f;
		float num2 = 0f;
		int num3 = 0;
		while ((float)num3 < 60.96f && num < 4.2672f)
		{
			int num4 = ((num3 == 0) ? 20 : 3);
			num3 += num4;
			try
			{
				location = LocationByMoving(location, num4);
				location2 = LocationByMoving(location2, num4);
				num = Vector3.Distance(GetPosition(location), GetPosition(location2));
				if (num - num2 < 0.1f)
				{
					return num3;
				}
				num2 = Mathf.Max(num, num2);
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Exception while finding fouling distance at distance {distance} {node}", num3, node);
				return num3;
			}
		}
		if (num < 4.2672f)
		{
			Debug.LogError($"Failed to find adequate clearance after {num3}, best was {num}", node);
		}
		return num3;
	}

	public bool IsWithinFoulingDistance(Location location)
	{
		if (!CheckForFouling(location))
		{
			return CheckForFouling(location.Flipped());
		}
		return true;
		bool CheckForFouling(Location cursor)
		{
			float num = 0f;
			while (num < 60.96f)
			{
				TrackSegment segment = cursor.segment;
				TrackNode node = segment.NodeForEnd(cursor.end.Flipped());
				float num2 = segment.DistanceBetween(node, cursor);
				num += num2;
				if (num > 60.96f)
				{
					return false;
				}
				if (DecodeSwitchAt(node, out var enter, out var _, out var _))
				{
					if (enter == segment)
					{
						return false;
					}
					return CalculateFoulingDistance(node) > num;
				}
				cursor = LocationByMoving(cursor, num2 + 0.1f, checkSwitchAgainstMovement: false, stopAtEndOfTrack: true);
				num += 0.1f;
				if (cursor.segment == segment)
				{
					return false;
				}
			}
			return false;
		}
	}

	public Location ResolveLocationString(string locStr)
	{
		string[] array = locStr.Split("|");
		if (array.Length != 3)
		{
			throw new BadLocationException("Malformed location string: \"" + locStr + "\"");
		}
		string id = array[0];
		string text = array[1];
		string s = array[2];
		TrackSegment segment = GetSegment(id);
		if (segment == null)
		{
			throw new BadLocationException("Segment not found: \"" + locStr + "\"");
		}
		TrackSegment.End end;
		if (!(text == "a"))
		{
			if (!(text == "b"))
			{
				throw new BadLocationException("Invalid end: \"" + locStr + "\"");
			}
			end = TrackSegment.End.B;
		}
		else
		{
			end = TrackSegment.End.A;
		}
		TrackSegment.End end2 = end;
		if (!float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
		{
			throw new BadLocationException("Malformed distance: \"" + locStr + "\"");
		}
		return new Location(segment, result, end2);
	}

	public string LocationToString(Location loc)
	{
		float distance = loc.distance;
		string text = distance.ToString("F3", CultureInfo.InvariantCulture);
		return loc.segment.id + "|" + ((loc.end == TrackSegment.End.A) ? "a" : "b") + "|" + text;
	}

	public void DebugPing(Location loc, Color color, float duration, float height)
	{
	}

	public Location ClosestLocationFacing(Location a, Location b, Location target)
	{
		PositionRotation positionRotation = GetPositionRotation(a);
		PositionRotation positionRotation2 = GetPositionRotation(b);
		Vector3 position = GetPosition(target);
		bool num = Vector3.SqrMagnitude(positionRotation.Position - position) < Vector3.SqrMagnitude(positionRotation2.Position - position);
		Location result = (num ? a : b);
		Vector3 vector = (num ? positionRotation.Position : positionRotation2.Position);
		Quaternion quaternion = (num ? positionRotation.Rotation : positionRotation2.Rotation);
		Vector3 vector2 = vector + quaternion * (Vector3.forward * (Vector3.Distance(vector, position) * 0.5f));
		if (!(Vector3.SqrMagnitude(vector - position) < Vector3.SqrMagnitude(vector2 - position)))
		{
			return result;
		}
		return result.Flipped();
	}
}
