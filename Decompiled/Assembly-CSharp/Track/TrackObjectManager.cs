using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core;
using Helpers;
using Serilog;
using UnityEngine;

namespace Track;

[ExecuteInEditMode]
[RequireComponent(typeof(Graph))]
[RequireComponent(typeof(TrackRebuilder))]
public class TrackObjectManager : MonoBehaviour
{
	private struct Descriptors
	{
		public Dictionary<string, SegmentDescriptor> segments;

		public Dictionary<string, SwitchDescriptor> switches;

		public Dictionary<string, BumperDescriptor> bumpers;
	}

	private readonly struct SegmentDescriptor : ITrackDescriptor
	{
		public readonly SegmentProxy segment;

		public string Identifier { get; }

		public SegmentDescriptor(SegmentProxy segment)
		{
			this.segment = segment;
			Identifier = $"segment-{segment.Segment.id}-{segment.Curve.EndPoint1:F1}-{segment.Curve.EndPoint2:F1}";
		}

		public GameObject BuildGameObject(TrackObjectBuilder builder)
		{
			return builder.CreateSegmentObject(segment);
		}

		public GameObject BuildMaskObject(TrackObjectBuilder builder)
		{
			return builder.CreateSegmentMasks(segment);
		}

		public override string ToString()
		{
			return Identifier;
		}
	}

	private readonly struct BumperDescriptor : ITrackDescriptor
	{
		public readonly TrackNode node;

		public readonly Vector3 direction;

		public readonly TrackSegment.Style style;

		public string Identifier { get; }

		public BumperDescriptor(TrackNode node, Vector3 direction, TrackSegment.Style style)
		{
			this.node = node;
			this.direction = direction;
			this.style = style;
			Identifier = "bumper-" + node.id;
		}

		public GameObject BuildGameObject(TrackObjectBuilder builder)
		{
			return builder.CreateBumperObject(node, direction, style);
		}

		public GameObject BuildMaskObject(TrackObjectBuilder builder)
		{
			return builder.CreateBumperMasks(node, direction, style);
		}

		public override string ToString()
		{
			return Identifier;
		}
	}

	private readonly struct SwitchDescriptor : ITrackDescriptor
	{
		public readonly SwitchGeometry geometry;

		public readonly TrackNode node;

		public readonly SegmentProxy aProxy;

		public readonly SegmentProxy bProxy;

		public readonly BezierCurve aRoadbedCurve;

		public readonly BezierCurve bRoadbedCurve;

		public readonly TrackSegment.Style trackSegmentStyle;

		public string Identifier { get; }

		public SwitchDescriptor(SwitchGeometry geometry, TrackNode node, SegmentProxy aProxy, SegmentProxy bProxy, BezierCurve aRoadbedCurve, BezierCurve bRoadbedCurve, TrackSegment.Style trackSegmentStyle)
		{
			this.geometry = geometry;
			this.node = node;
			this.aProxy = aProxy;
			this.bProxy = bProxy;
			this.aRoadbedCurve = aRoadbedCurve;
			this.bRoadbedCurve = bRoadbedCurve;
			this.trackSegmentStyle = trackSegmentStyle;
			Identifier = "switch-" + node.id;
		}

		public GameObject BuildGameObject(TrackObjectBuilder builder)
		{
			return builder.CreateSwitchObject(geometry, node, aProxy.Curve, aRoadbedCurve, bProxy.Curve, bRoadbedCurve);
		}

		public GameObject BuildMaskObject(TrackObjectBuilder builder)
		{
			return builder.CreateSwitchMasks(geometry, node, aProxy.Curve, aRoadbedCurve, bProxy.Curve, bRoadbedCurve, trackSegmentStyle);
		}

		public override string ToString()
		{
			return Identifier;
		}
	}

	public interface ITrackDescriptor
	{
		string Identifier { get; }

		GameObject BuildGameObject(TrackObjectBuilder builder);

		GameObject BuildMaskObject(TrackObjectBuilder builder);
	}

	public TrackMeshProfile profile;

	[SerializeField]
	private PrefabInstancer prefabInstancer;

	public bool hideObjects = true;

	private ITrackRebuilder _rebuilder;

	private Graph _graph;

	private TrackObjectBuilder _builder;

	private Descriptors _descriptors;

	private static TrackObjectManager _instance;

	private readonly HashSet<TrackNode> _pendingNodes = new HashSet<TrackNode>();

	private CoroutineTask _invalidateCoroutine;

	private int _changeCount;

	public Graph Graph => _graph;

	public static TrackObjectManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = UnityEngine.Object.FindObjectOfType<TrackObjectManager>();
			}
			return _instance;
		}
	}

	private void Awake()
	{
		_graph = GetComponent<Graph>();
	}

	private void Reset()
	{
		Rebuild();
	}

	private void OnEnable()
	{
		HideFlags meshHideFlags = (hideObjects ? HideFlags.HideAndDontSave : HideFlags.DontSave);
		_builder = new TrackObjectBuilder(profile, base.gameObject, prefabInstancer, meshHideFlags);
		if (Application.isPlaying)
		{
			_rebuilder = GetComponent<TrackRebuilder>();
		}
		_rebuilder.BuildGameObject = BuildGameObject;
		_rebuilder.BuildMaskObject = BuildMaskObject;
		Log.Debug("Rebuilder = {rebuilder}", _rebuilder);
		_graph.NodeDidChange += GraphOnNodeDidChange;
		CoroutineTask.Start(RebuildCoroutine(), this);
	}

	private void OnDisable()
	{
		_invalidateCoroutine?.Stop();
		_rebuilder = null;
		_builder = null;
		_graph.NodeDidChange -= GraphOnNodeDidChange;
		_ = Application.isPlaying;
	}

	private IEnumerator RebuildCoroutine()
	{
		yield return null;
		while (Camera.main == null && Application.isPlaying)
		{
			yield return new WaitForSeconds(0.1f);
		}
		try
		{
			Rebuild();
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Error rebuilding in OnEnable");
		}
	}

	private void DestroyMesh()
	{
		base.transform.FindObjectsWithTag("TrackMeshGenerated").ForEach(UnityEngine.Object.DestroyImmediate);
	}

	public void SetNeedsRebuild(TrackNode node)
	{
		Graph.OnNodeDidChange(node);
	}

	public void Rebuild()
	{
		if (_rebuilder == null)
		{
			Log.Warning("Rebuilder is null.");
			return;
		}
		Log.Debug("Rebuild");
		_rebuilder.Clear();
		DestroyMesh();
		_graph.RebuildCollections();
		_descriptors = BuildDescriptors(_graph.Nodes, _graph);
		foreach (SegmentDescriptor value in _descriptors.segments.Values)
		{
			_rebuilder.Add(value, value.segment.Curve);
		}
		foreach (SwitchDescriptor value2 in _descriptors.switches.Values)
		{
			BoundingSphere boundingSphere = CalculateBoundingSphereForSwitch(value2);
			_rebuilder.Add(value2, boundingSphere);
		}
		foreach (BumperDescriptor value3 in _descriptors.bumpers.Values)
		{
			_rebuilder.Add(value3, new BoundingSphere(value3.node.transform.localPosition, 10f));
		}
	}

	private GameObject BuildGameObject(ITrackDescriptor descriptor)
	{
		return descriptor.BuildGameObject(_builder);
	}

	private GameObject BuildMaskObject(ITrackDescriptor descriptor)
	{
		return descriptor.BuildMaskObject(_builder);
	}

	private static Descriptors BuildDescriptors(IEnumerable<TrackNode> nodes, Graph graph)
	{
		Dictionary<string, SwitchDescriptor> dictionary = new Dictionary<string, SwitchDescriptor>();
		Dictionary<string, BumperDescriptor> dictionary2 = new Dictionary<string, BumperDescriptor>();
		Dictionary<string, SegmentDescriptor> dictionary3 = new Dictionary<string, SegmentDescriptor>();
		HashSet<TrackNode> hashSet = new HashSet<TrackNode>(nodes);
		HashSet<SegmentProxy> hashSet2 = new HashSet<SegmentProxy>(from segment in graph.SegmentsAffectedByNodes(hashSet)
			select new SegmentProxy(segment));
		foreach (TrackNode item in hashSet)
		{
			Vector3 nodePosition = item.transform.localPosition;
			Vector3 direction;
			if (graph.DecodeSwitchAt(item, out var _, out var segmentA, out var segmentB))
			{
				List<SegmentProxy> source = hashSet2.Where((SegmentProxy s) => s.Curve.EndPoint1 == nodePosition || s.Curve.EndPoint2 == nodePosition).ToList();
				TrackSegment.Style trackSegmentStyle = ((segmentA.style == TrackSegment.Style.Yard || segmentB.style == TrackSegment.Style.Yard) ? TrackSegment.Style.Yard : TrackSegment.Style.Standard);
				try
				{
					SegmentProxy aProxy = source.First((SegmentProxy s) => ProxyIsSubsegmentOf(s, segmentA));
					SegmentProxy segmentProxy = source.First((SegmentProxy s) => ProxyIsSubsegmentOf(s, segmentB) && !aProxy.Equals(s));
					hashSet2.Remove(aProxy);
					hashSet2.Remove(segmentProxy);
					SegmentProxy sliceA;
					SegmentProxy sliceB;
					List<SegmentProxy> remainder;
					SwitchGeometry geometry = SwitchGeometry.Calculate(item, aProxy, segmentProxy, out sliceA, out sliceB, out remainder);
					hashSet2.UnionWith(remainder);
					SwitchDescriptor value = new SwitchDescriptor(geometry, item, aProxy, segmentProxy, sliceA.Curve, sliceB.Curve, trackSegmentStyle);
					dictionary[value.Identifier] = value;
				}
				catch (Exception)
				{
					Debug.LogError("Error calculating switch " + item.id + " geometry:", item);
				}
			}
			else if (graph.NodeIsDeadEnd(item, out direction))
			{
				TrackSegment trackSegment = graph.SegmentsConnectedTo(item).FirstOrDefault();
				if (trackSegment != null && item.turntable == null)
				{
					BumperDescriptor value2 = new BumperDescriptor(item, direction, trackSegment.style);
					dictionary2[value2.Identifier] = value2;
				}
			}
		}
		foreach (SegmentProxy item2 in hashSet2)
		{
			if (!item2.Segment.IsInvisible)
			{
				SegmentDescriptor value3 = new SegmentDescriptor(item2);
				dictionary3[value3.Identifier] = value3;
			}
		}
		Log.Information("Rebuild returning {segments} segments, {switches} switches, and {bumpers} bumpers.", dictionary3.Count, dictionary.Count, dictionary2.Count);
		return new Descriptors
		{
			segments = dictionary3,
			switches = dictionary,
			bumpers = dictionary2
		};
		static bool ProxyIsSubsegmentOf(SegmentProxy proxy, TrackSegment segment)
		{
			return (object)proxy.Segment == segment;
		}
	}

	private void GraphOnNodeDidChange(TrackNode node)
	{
		_pendingNodes.Add(node);
		_changeCount++;
		if (_invalidateCoroutine == null)
		{
			_invalidateCoroutine = CoroutineTask.Start(InvalidatePendingAfterDelay(), this);
		}
	}

	private IEnumerator InvalidatePendingAfterDelay()
	{
		int lastChangeCount;
		do
		{
			lastChangeCount = _changeCount;
			yield return new WaitForSecondsRealtime(0.1f);
		}
		while (lastChangeCount != _changeCount);
		Log.Debug("TrackObjectManager: Invalidate {nodes} nodes", _pendingNodes);
		_invalidateCoroutine = null;
		InvalidateFromNode(_pendingNodes, out var _);
		_pendingNodes.Clear();
	}

	private void InvalidateFromNode(HashSet<TrackNode> nodes, out Bounds affectedBounds)
	{
		if (_descriptors.segments == null)
		{
			affectedBounds = default(Bounds);
			return;
		}
		affectedBounds = BoundsForNode(nodes.First());
		HashSet<SegmentDescriptor> affectedSegments = _descriptors.segments.Values.Where((SegmentDescriptor desc) => nodes.Any((TrackNode node) => desc.segment.Segment.Contains(node))).ToHashSet();
		HashSet<SwitchDescriptor> affectedSwitches = _descriptors.switches.Values.Where((SwitchDescriptor desc) => nodes.Any((TrackNode node) => desc.aProxy.Segment.Contains(node) || desc.bProxy.Segment.Contains(node))).ToHashSet();
		HashSet<BumperDescriptor> affectedBumpers = _descriptors.bumpers.Values.Where((BumperDescriptor desc) => nodes.Contains(desc.node)).ToHashSet();
		foreach (SegmentDescriptor item in affectedSegments)
		{
			_rebuilder.Remove(item);
			_descriptors.segments.Remove(item.Identifier);
			affectedBounds.Encapsulate(BoundsForNode(item.segment.Segment.a));
			affectedBounds.Encapsulate(BoundsForNode(item.segment.Segment.b));
		}
		foreach (SwitchDescriptor item2 in affectedSwitches)
		{
			_rebuilder.Remove(item2);
			_descriptors.switches.Remove(item2.Identifier);
		}
		foreach (BumperDescriptor item3 in affectedBumpers)
		{
			_rebuilder.Remove(item3);
			_descriptors.bumpers.Remove(item3.Identifier);
		}
		HashSet<TrackNode> hashSet = new HashSet<TrackNode>();
		hashSet.UnionWith(affectedSegments.SelectMany((SegmentDescriptor desc) => new TrackNode[2]
		{
			desc.segment.Segment.a,
			desc.segment.Segment.b
		}));
		hashSet.UnionWith(affectedSwitches.SelectMany((SwitchDescriptor desc) => new TrackNode[4]
		{
			desc.aProxy.Segment.a,
			desc.aProxy.Segment.b,
			desc.bProxy.Segment.a,
			desc.bProxy.Segment.b
		}));
		hashSet.UnionWith(affectedBumpers.Select((BumperDescriptor desc) => desc.node));
		Descriptors descriptors = BuildDescriptors(hashSet, _graph);
		foreach (SegmentDescriptor item4 in descriptors.segments.Values.Where((SegmentDescriptor descriptor) => affectedSegments.Any((SegmentDescriptor s) => s.segment.Segment == descriptor.segment.Segment)))
		{
			_rebuilder.Add(item4, item4.segment.Curve);
			_descriptors.segments[item4.Identifier] = item4;
			affectedBounds.Encapsulate(BoundsForNode(item4.segment.Segment.a));
			affectedBounds.Encapsulate(BoundsForNode(item4.segment.Segment.b));
		}
		foreach (SwitchDescriptor item5 in descriptors.switches.Values.Where((SwitchDescriptor descriptor) => affectedSwitches.Any((SwitchDescriptor s) => s.node == descriptor.node)))
		{
			BoundingSphere boundingSphere = CalculateBoundingSphereForSwitch(item5);
			_rebuilder.Add(item5, boundingSphere);
			_descriptors.switches[item5.Identifier] = item5;
		}
		foreach (BumperDescriptor item6 in descriptors.bumpers.Values.Where((BumperDescriptor descriptor) => affectedBumpers.Any((BumperDescriptor s) => s.node == descriptor.node)))
		{
			_rebuilder.Add(item6, new BoundingSphere(item6.node.transform.localPosition, 10f));
			_descriptors.bumpers[item6.Identifier] = item6;
		}
		static Bounds BoundsForNode(TrackNode node)
		{
			return new Bounds(node.transform.localPosition, Vector3.one * 20f);
		}
	}

	private static BoundingSphere CalculateBoundingSphereForSwitch(SwitchDescriptor sw)
	{
		return CalculateBoundingSphereForNodes(new TrackNode[4]
		{
			sw.aProxy.Segment.a,
			sw.aProxy.Segment.b,
			sw.bProxy.Segment.a,
			sw.bProxy.Segment.b
		});
	}

	private static BoundingSphere CalculateBoundingSphereForNodes(TrackNode[] nodes)
	{
		Bounds bounds = new Bounds(nodes[0].transform.localPosition, Vector3.zero);
		for (int i = 1; i < nodes.Length; i++)
		{
			bounds.Encapsulate(nodes[i].transform.localPosition);
		}
		return new BoundingSphere(bounds.center, bounds.extents.magnitude + 10f);
	}
}
