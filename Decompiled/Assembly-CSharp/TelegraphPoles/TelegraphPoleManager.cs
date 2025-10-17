using System;
using System.Collections;
using System.Collections.Generic;
using Helpers;
using Map.Runtime;
using Map.Runtime.MapModifiers;
using Map.Runtime.MaskComponents;
using SimpleGraph.Runtime;
using UnityEngine;

namespace TelegraphPoles;

[SelectionBase]
[ExecuteInEditMode]
[RequireComponent(typeof(SimpleGraph.Runtime.SimpleGraph))]
public class TelegraphPoleManager : MonoBehaviour
{
	private class WireSet
	{
		public List<TelegraphWire> Wires = new List<TelegraphWire>();
	}

	[SerializeField]
	private List<TelegraphPole> polePrefabs = new List<TelegraphPole>();

	[SerializeField]
	private TelegraphWire wirePrefab;

	private readonly HashSet<int> _enabledTags = new HashSet<int>();

	private Coroutine _rebuildCoroutine;

	[Header("Debug")]
	[SerializeField]
	public bool debugDrawHeights;

	private SimpleGraph.Runtime.SimpleGraph _graph;

	private readonly Dictionary<int, TelegraphPole> _instances = new Dictionary<int, TelegraphPole>();

	private CullingGroup _cullingGroup;

	private BoundingSphere[] _spheres = Array.Empty<BoundingSphere>();

	private readonly Dictionary<int, int> _sphereLookup = new Dictionary<int, int>();

	private readonly float[] _cullingDistances = new float[2] { 100f, 500f };

	private const int MinDistanceBand = 1;

	private readonly Dictionary<(int, int), WireSet> _wireSets = new Dictionary<(int, int), WireSet>();

	private SimpleGraph.Runtime.SimpleGraph Graph
	{
		get
		{
			if (_graph == null)
			{
				_graph = GetComponent<SimpleGraph.Runtime.SimpleGraph>();
			}
			return _graph;
		}
	}

	private void OnEnable()
	{
		SimpleGraph.Runtime.SimpleGraph graph = Graph;
		graph.RebuildIfNeeded();
		_cullingGroup = new CullingGroup();
		_cullingGroup.SetBoundingSpheres(_spheres);
		_cullingGroup.SetBoundingSphereCount(0);
		_cullingGroup.onStateChanged = CullingGroupStateChanged;
		_cullingGroup.SetBoundingDistances(_cullingDistances);
		_cullingGroup.AutoAssignTargetCamera(this);
		if (WorldTransformer.TryGetShared(out var shared))
		{
			shared.OnDidMove += OnWorldDidMove;
		}
		graph.OnNodeChanged += NodeDidChange;
		graph.OnNodeEdgeAddRemove += Rebuild;
		base.transform.DestroyAllChildren();
		_instances.Clear();
		Rebuild();
	}

	private void OnDisable()
	{
		SimpleGraph.Runtime.SimpleGraph graph = Graph;
		graph.OnNodeChanged -= NodeDidChange;
		graph.OnNodeEdgeAddRemove -= Rebuild;
		if (WorldTransformer.TryGetShared(out var shared))
		{
			shared.OnDidMove -= OnWorldDidMove;
		}
		_cullingGroup.Dispose();
		_cullingGroup = null;
	}

	private void OnDestroy()
	{
		DestroyInstances();
	}

	public void SetTagEnabled(int poleTag, bool enable)
	{
		if (enable)
		{
			_enabledTags.Add(poleTag);
		}
		else
		{
			_enabledTags.Remove(poleTag);
		}
		RebuildAfterFrame();
	}

	private void RebuildAfterFrame()
	{
		if (_rebuildCoroutine == null && base.gameObject.activeInHierarchy)
		{
			_rebuildCoroutine = StartCoroutine(RebuildAfterFrameCoroutine());
		}
	}

	private IEnumerator RebuildAfterFrameCoroutine()
	{
		yield return null;
		_rebuildCoroutine = null;
		Rebuild();
	}

	public bool TryGetPole(int nodeId, out TelegraphPole pole)
	{
		return _instances.TryGetValue(nodeId, out pole);
	}

	private void OnWorldDidMove(Vector3 offset)
	{
		for (int i = 0; i < _spheres.Length; i++)
		{
			_spheres[i].position += offset;
		}
		foreach (WireSet value in _wireSets.Values)
		{
			foreach (TelegraphWire wire in value.Wires)
			{
				wire.WorldDidMove(offset);
			}
		}
	}

	private bool TryGetNodeIdForSphereIndex(int sphereIndex, out int nodeId)
	{
		foreach (var (num3, num4) in _sphereLookup)
		{
			if (sphereIndex == num4)
			{
				nodeId = num3;
				return true;
			}
		}
		nodeId = -1;
		return false;
	}

	private void CullingGroupStateChanged(CullingGroupEvent evt)
	{
		bool flag = evt.currentDistance <= 1;
		if (!TryGetNodeIdForSphereIndex(evt.index, out var nodeId))
		{
			Debug.LogWarning($"Couldn't find node for sphere {evt.index}");
			return;
		}
		bool flag2 = _instances.ContainsKey(nodeId);
		if (flag2 == flag)
		{
			return;
		}
		SimpleGraph.Runtime.SimpleGraph graph = Graph;
		Node node = graph.NodeForId(nodeId);
		if (flag2)
		{
			DestroyInstance(_instances[nodeId].gameObject);
			_instances.Remove(nodeId);
			{
				foreach (Edge item in graph.EnumerateEdgesFromTo(node))
				{
					Node b = item.Other(node);
					DestroyWiresBetween(node, b);
				}
				return;
			}
		}
		BuildPole(graph, node);
	}

	private void DestroyInstances()
	{
		foreach (TelegraphPole value in _instances.Values)
		{
			DestroyInstance(value.gameObject);
		}
		_instances.Clear();
		foreach (WireSet value2 in _wireSets.Values)
		{
			foreach (TelegraphWire wire in value2.Wires)
			{
				if (!(wire == null))
				{
					DestroyInstance(wire.gameObject);
				}
			}
		}
		_wireSets.Clear();
	}

	private static void DestroyInstance(GameObject go)
	{
		if (!(go == null))
		{
			if (Application.isPlaying)
			{
				UnityEngine.Object.Destroy(go);
			}
			else
			{
				UnityEngine.Object.DestroyImmediate(go);
			}
		}
	}

	private (int, int) TupleForNodes(Node a, Node b)
	{
		if (a.id >= b.id)
		{
			return (b.id, a.id);
		}
		return (a.id, b.id);
	}

	private void BuildWiresBetween(Node a, Node b)
	{
		(int, int) key = TupleForNodes(a, b);
		TelegraphPole telegraphPole = _instances[a.id];
		TelegraphPole telegraphPole2 = _instances[b.id];
		bool flag = Vector3.Dot(telegraphPole.transform.forward, telegraphPole2.transform.forward) > 0f;
		int num = Mathf.Min(telegraphPole.CountPoints(), telegraphPole2.CountPoints());
		List<TelegraphWire> wires = new List<TelegraphWire>();
		int num2 = telegraphPole.rows.Length - 1;
		int num3 = telegraphPole2.rows.Length - 1;
		int num4 = 0;
		int num5 = 0;
		for (int i = 0; i < num; i++)
		{
			Vector3 posA = telegraphPole.transform.TransformPoint(telegraphPole.rows[num2].points[num4]);
			Vector3 posB = telegraphPole2.transform.TransformPoint(telegraphPole2.rows[num3].points[flag ? num5 : (telegraphPole2.rows[num3].points.Length - 1 - num5)]);
			AddWire(posA, posB);
			num4++;
			num5++;
			if (num4 >= telegraphPole.rows[num2].points.Length)
			{
				num2--;
				num4 = 0;
			}
			if (num5 >= telegraphPole2.rows[num3].points.Length)
			{
				num3--;
				num5 = 0;
			}
		}
		_wireSets[key] = new WireSet
		{
			Wires = wires
		};
		void AddWire(Vector3 a2, Vector3 b2)
		{
			TelegraphWire telegraphWire = UnityEngine.Object.Instantiate(wirePrefab, base.transform);
			telegraphWire.gameObject.hideFlags = HideFlags.HideAndDontSave;
			telegraphWire.gameObject.name = $"Wire {a.id} {b.id}";
			telegraphWire.Configure(a2, b2);
			wires.Add(telegraphWire);
		}
	}

	private void DestroyWiresBetween(Node a, Node b)
	{
		(int, int) key = TupleForNodes(a, b);
		if (!_wireSets.TryGetValue(key, out var value))
		{
			return;
		}
		foreach (TelegraphWire wire in value.Wires)
		{
			if (!(wire == null))
			{
				DestroyInstance(wire.gameObject);
			}
		}
		_wireSets.Remove(key);
	}

	private bool ShouldShowNode(Node node)
	{
		if (_enabledTags.Count == 0)
		{
			return true;
		}
		return _enabledTags.Contains(node.tag);
	}

	[ContextMenu("Rebuild Poles")]
	private void Rebuild()
	{
		DestroyInstances();
		SimpleGraph.Runtime.SimpleGraph graph = Graph;
		_spheres = new BoundingSphere[graph.Nodes.Count];
		_cullingGroup.SetBoundingSpheres(_spheres);
		_cullingGroup.SetBoundingSphereCount(_spheres.Length);
		_sphereLookup.Clear();
		int num = 0;
		foreach (Node node in graph.Nodes)
		{
			if (ShouldShowNode(node))
			{
				Vector3 position = graph.WorldPositionForNode(node);
				_spheres[num].position = position;
				_spheres[num].radius = 20f;
				_sphereLookup[node.id] = num;
				num++;
				if (_cullingGroup.CalculateDistanceBand(position, _cullingDistances) <= 1)
				{
					BuildPole(graph, node);
				}
			}
		}
		_cullingGroup.SetBoundingSphereCount(num);
		RebuildMapMasks();
	}

	private void RebuildMapMasks()
	{
		StaticMapMask staticMapMask = GetComponent<StaticMapMask>() ?? base.gameObject.AddComponent<StaticMapMask>();
		staticMapMask.RemoveModifiers();
		foreach (Node node in Graph.Nodes)
		{
			if (ShouldShowNode(node))
			{
				RectangleMaskDescriptor rectangleMaskDescriptor = new RectangleMaskDescriptor(node.position.WorldToGame(), Vector3.one, 0f, 3f, 0f);
				staticMapMask.AddModifier(new MaskModifier(MaskName.CutTrees, 1f, rectangleMaskDescriptor));
			}
		}
		foreach (Edge edge in Graph.Edges)
		{
			Vector3 a = Graph.WorldPositionForNode(edge.A).WorldToGame();
			Vector3 b = Graph.WorldPositionForNode(edge.B).WorldToGame();
			Vector3 center = Vector3.Lerp(a, b, 0.5f);
			RectangleMaskDescriptor rectangleMaskDescriptor2 = new RectangleMaskDescriptor(bounds: new Bounds(center, new Vector3(Mathf.Max(Mathf.Abs(a.x - center.x), Mathf.Abs(b.x - center.x)) * 2f + 5f, Mathf.Max(Mathf.Abs(a.y - center.y), Mathf.Abs(b.y - center.y)) * 2f + 5f, Mathf.Max(Mathf.Abs(a.z - center.z), Mathf.Abs(b.z - center.z)) * 2f + 5f)), a: a, b: b, thickness: 5f, radius: 0f, falloff: 0f);
			staticMapMask.AddModifier(new MaskModifier(MaskName.CutTrees, 1f, rectangleMaskDescriptor2));
		}
	}

	private void NodeDidChange(int nodeId)
	{
		SimpleGraph.Runtime.SimpleGraph graph = Graph;
		Node node = graph.NodeForId(nodeId);
		if (_instances.TryGetValue(node.id, out var value) && value != null)
		{
			GetPositionRotationForNode(graph, node, out var position, out var rotation, out var basePosition);
			value.transform.SetPositionAndRotation(position, rotation);
			value.localBasePosition = basePosition - position;
		}
		else
		{
			BuildPole(graph, node);
		}
		if (_sphereLookup.TryGetValue(nodeId, out var value2))
		{
			_spheres[value2].position = graph.WorldPositionForNode(node);
		}
	}

	private void BuildPole(SimpleGraph.Runtime.SimpleGraph graph, Node node)
	{
		if (_instances.TryGetValue(node.id, out var value) && value != null)
		{
			UnityEngine.Object.DestroyImmediate(value.gameObject);
		}
		GetPositionRotationForNode(graph, node, out var position, out var rotation, out var basePosition);
		TelegraphPole telegraphPole = UnityEngine.Object.Instantiate(polePrefabs.Random(), position, rotation, base.transform);
		telegraphPole.localBasePosition = basePosition - position;
		GameObject gameObject = telegraphPole.gameObject;
		gameObject.name = $"{gameObject.name} {node.id}";
		gameObject.hideFlags = HideFlags.DontSave;
		_instances[node.id] = telegraphPole;
		foreach (Edge item in graph.EnumerateEdgesFromTo(node))
		{
			Node node2 = item.Other(node);
			if (_instances.ContainsKey(node2.id))
			{
				BuildWiresBetween(node, node2);
			}
		}
	}

	private static void GetPositionRotationForNode(SimpleGraph.Runtime.SimpleGraph graph, Node node, out Vector3 position, out Quaternion rotation, out Vector3 basePosition)
	{
		rotation = graph.WorldRotationForNode(node);
		position = graph.WorldPositionForNode(node);
		basePosition = position;
		position += rotation * Vector3.up * Mathf.LerpUnclamped(-10f, 0f, node.scale.y);
	}
}
