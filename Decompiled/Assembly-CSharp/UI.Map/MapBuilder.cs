using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using CorgiSpline;
using GalaSoft.MvvmLight.Messaging;
using Game;
using Game.Events;
using Game.Settings;
using Game.State;
using Helpers;
using Track;
using UnityEngine;

namespace UI.Map;

public class MapBuilder : MonoBehaviour
{
	private class Entry
	{
		public readonly TrackSegment Segment;

		public byte Visibility;

		public Entry(TrackSegment segment)
		{
			Segment = segment;
			Visibility = 0;
		}
	}

	public Camera mapCamera;

	[SerializeField]
	private Material splineMaterial;

	[SerializeField]
	private MapSwitchStand mapSwitchStandPrefab;

	public float segmentLineWidthMax = 40f;

	public float segmentLineWidthMin = 2f;

	[Tooltip("Assigned by MapWindow to reflect the size of the window, as the window/texture size affect the min/max size we want to allow.")]
	public float windowSizeFactor = 1f;

	private const float MinOrthoSize = 100f;

	private const float MaxOrthoSize = 10000f;

	[SerializeField]
	private Color trackTint = Color.white;

	[SerializeField]
	private float trackMultMainline = 0.7f;

	[SerializeField]
	private float trackMultBranch = 0.6f;

	[SerializeField]
	private float trackMultIndustrial = 0.5f;

	[SerializeField]
	private float trackMultUnavailable = 0.3f;

	private float _segmentLineWidth = 2f;

	private static int _mapLayer;

	private Transform _container;

	private CullingGroup _cullingGroup;

	private BoundingSphere[] _cullingSpheres = Array.Empty<BoundingSphere>();

	private readonly Dictionary<string, SplineMeshBuilder> _splines = new Dictionary<string, SplineMeshBuilder>();

	private List<Entry> _entries;

	private HashSet<MapIcon> _mapIcons = new HashSet<MapIcon>();

	private HashSet<MapLabel> _mapLabels;

	private readonly Queue<Material> _materials = new Queue<Material>();

	private static MapBuilder _shared;

	private readonly Dictionary<string, MapSwitchStand> _switchStands = new Dictionary<string, MapSwitchStand>();

	private IDisposable _mapShowsSwitchesObserver;

	private bool _mapShowsSwitches;

	private Color TrackColorMainline => trackTint * trackMultMainline;

	private Color TrackColorBranch => trackTint * trackMultBranch;

	private Color TrackColorIndustrial => trackTint * trackMultIndustrial;

	private Color TrackColorUnavailable => trackTint * trackMultUnavailable;

	public static MapBuilder Shared
	{
		get
		{
			if (_shared == null)
			{
				_shared = UnityEngine.Object.FindObjectOfType<MapBuilder>();
			}
			return _shared;
		}
	}

	private float NormalizedScale => Mathf.InverseLerp(100f, 10000f, mapCamera.orthographicSize);

	private float IconScale => Mathf.Lerp(0.2f, 8f, NormalizedScale);

	private bool ShowSwitchStands { get; set; }

	private Vector3 SplineScaleMult => new Vector3(_segmentLineWidth, _segmentLineWidth, 1f);

	public void Add(MapIcon icon)
	{
		_mapIcons.Add(icon);
		icon.SetZoom(IconScale);
	}

	public void Remove(MapIcon icon)
	{
		_mapIcons.Remove(icon);
	}

	private void Awake()
	{
		_mapLayer = Layers.Map;
	}

	private void OnEnable()
	{
		_mapIcons = UnityEngine.Object.FindObjectsOfType<MapIcon>(includeInactive: true).ToHashSet();
		_mapLabels = null;
		Messenger.Default.Register(this, delegate(SwitchThrownDidChange evt)
		{
			SwitchThrownDidChange(evt.Node);
		});
		Messenger.Default.Register<CanvasScaleChanged>(this, delegate
		{
			Rebuild();
		});
		GameStorage storage = StateManager.Shared.Storage;
		_mapShowsSwitches = storage.MapShowsSwitches;
		_mapShowsSwitchesObserver = storage.ObserveMapShowsSwitches(delegate(bool value)
		{
			_mapShowsSwitches = value;
			Rebuild();
		}, callInitial: false);
	}

	private void OnDisable()
	{
		Messenger.Default.Unregister(this);
		_mapShowsSwitchesObserver?.Dispose();
	}

	private void Update()
	{
		if (base.transform.hasChanged && mapCamera.gameObject.activeSelf)
		{
			if (_cullingGroup != null)
			{
				UpdateCullingSpheres();
			}
			base.transform.hasChanged = false;
		}
	}

	private void OnDestroy()
	{
		_cullingGroup?.Dispose();
		_cullingGroup = null;
		DestroyAllSplines();
		TrimMaterialPool(0);
	}

	public void SetVisible(bool visible)
	{
		mapCamera.gameObject.SetActive(visible);
		if (_container != null)
		{
			_container.gameObject.SetActive(visible);
		}
		if (!visible)
		{
			TrimMaterialPool(8);
		}
	}

	public void Zoom(float delta, Vector2 viewportMousePosition)
	{
		float orthographicSize = mapCamera.orthographicSize;
		float num = Mathf.Sqrt(orthographicSize) * 5f;
		float value = orthographicSize + delta * num;
		value = Mathf.Clamp(value, 100f * windowSizeFactor, 10000f * windowSizeFactor);
		Vector3 position = new Vector3(viewportMousePosition.x, viewportMousePosition.y, mapCamera.nearClipPlane);
		Vector3 vector = mapCamera.ViewportToWorldPoint(position);
		mapCamera.orthographicSize = value;
		Vector3 vector2 = mapCamera.ViewportToWorldPoint(position);
		mapCamera.transform.localPosition += vector - vector2;
		UpdateForZoom();
	}

	private void UpdateForZoom()
	{
		float normalizedScale = NormalizedScale;
		_segmentLineWidth = Mathf.Lerp(segmentLineWidthMin, segmentLineWidthMax, normalizedScale);
		foreach (SplineMeshBuilder value in _splines.Values)
		{
			value.scaleMult = SplineScaleMult;
			value.ForceImmediateRebuild();
		}
		if (_mapLabels == null)
		{
			_mapLabels = UnityEngine.Object.FindObjectsOfType<MapLabel>(includeInactive: true).ToHashSet();
		}
		float iconScale = IconScale;
		foreach (MapIcon mapIcon in _mapIcons)
		{
			if (!(mapIcon == null))
			{
				mapIcon.SetZoom(iconScale);
			}
		}
		foreach (MapLabel mapLabel in _mapLabels)
		{
			if (!(mapLabel == null))
			{
				mapLabel.SetZoom(iconScale * 4f);
			}
		}
		UpdateShowSwitchStands();
		bool showSwitchStands = ShowSwitchStands;
		foreach (MapSwitchStand value2 in _switchStands.Values)
		{
			if (!(value2 == null))
			{
				value2.gameObject.SetActive(showSwitchStands);
			}
		}
	}

	private void UpdateShowSwitchStands()
	{
		float graphicsCanvasScale = Preferences.GraphicsCanvasScale;
		float num = 2f * mapCamera.orthographicSize / ((float)Screen.height * graphicsCanvasScale);
		float num2 = 4f / num;
		ShowSwitchStands = num2 > 8f;
	}

	public void SetMapCenter(Vector3 gamePosition)
	{
		mapCamera.transform.localPosition = new Vector3(gamePosition.x, 5000f, gamePosition.z);
	}

	public void Rebuild()
	{
		Graph graph = TrainController.Shared.graph;
		CreateContainerIfNeeded();
		DestroyAllSplines();
		_cullingGroup?.Dispose();
		_cullingGroup = new CullingGroup();
		_cullingGroup.targetCamera = mapCamera;
		_cullingGroup.SetBoundingSphereCount(0);
		_cullingGroup.SetBoundingSpheres(_cullingSpheres);
		_cullingGroup.onStateChanged = CullingGroupStateChanged;
		_cullingGroup.SetBoundingDistances(new float[1] { float.PositiveInfinity });
		_cullingGroup.SetDistanceReferencePoint(mapCamera.transform);
		_cullingSpheres = new BoundingSphere[(_entries = graph.Segments.Select((TrackSegment s) => new Entry(s)).ToList()).Count * 4];
		UpdateCullingSpheres();
		_cullingGroup.SetBoundingSpheres(_cullingSpheres);
		_cullingGroup.SetBoundingSphereCount(_cullingSpheres.Length);
	}

	private void UpdateCullingSpheres()
	{
		for (int i = 0; i < _entries.Count; i++)
		{
			BezierCurve curve = _entries[i].Segment.Curve;
			for (int j = 0; j < 4; j++)
			{
				Vector3 pos = WorldTransformer.GameToWorld(curve.GetPoint(j));
				_cullingSpheres[i * 4 + j] = new BoundingSphere(pos, 1f);
			}
		}
	}

	private void BuildSpline(TrackSegment segment)
	{
		if (!_splines.ContainsKey(segment.id))
		{
			GameObject gameObject = new GameObject
			{
				name = segment.name,
				hideFlags = HideFlags.DontSave,
				layer = _mapLayer
			};
			gameObject.SetActive(value: false);
			gameObject.transform.SetParent(_container, worldPositionStays: false);
			gameObject.AddComponent<MeshFilter>();
			BezierCurve curve = segment.Curve;
			Vector3 vector = Vector3.up * segment.priority;
			gameObject.transform.localPosition = curve.P0;
			vector -= curve.P0;
			Color color = Grayscale(0f);
			Color color2 = Grayscale(0.4f);
			Color color3 = Grayscale(0.6f);
			Color color4 = Grayscale(1f);
			Vector3 pointOffsetA = Vector3.down * 10f;
			Vector3 pointOffsetB = Vector3.down * 10f;
			float thresholdA = -1f;
			float thresholdB = 2f;
			if (_mapShowsSwitches)
			{
				GetSwitchAdjustments(segment, gameObject.transform, ref pointOffsetA, ref pointOffsetB, ref thresholdA, ref thresholdB);
			}
			MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
			Material sharedMaterial = MaterialForSegment(segment, thresholdA, thresholdB);
			meshRenderer.sharedMaterial = sharedMaterial;
			Spline spline = gameObject.AddComponent<Spline>();
			Vector3 position = curve.P0 + vector + pointOffsetA;
			Vector3 position2 = curve.P1 + vector + pointOffsetA;
			Vector3 position3 = curve.P2 + vector + pointOffsetB;
			Vector3 position4 = curve.P3 + vector + pointOffsetB;
			spline.Points = new SplinePoint[4]
			{
				new SplinePoint(position, Quaternion.identity, Vector3.one, color),
				new SplinePoint(position2, Quaternion.identity, Vector3.one, color2),
				new SplinePoint(position3, Quaternion.identity, Vector3.one, color3),
				new SplinePoint(position4, Quaternion.identity, Vector3.one, color4)
			};
			spline.SetSplineMode(SplineMode.Bezier);
			spline.SetSplineSpace(Space.Self, updatePoints: false);
			spline.UpdateNativeArrayOnEnable = true;
			SplineMeshBuilder splineMeshBuilder = gameObject.AddComponent<SplineMeshBuilder>();
			splineMeshBuilder.SplineReference = spline;
			splineMeshBuilder.AllowAsyncRebuild = true;
			splineMeshBuilder.RebuildOnEnable = true;
			splineMeshBuilder.scaleMult = SplineScaleMult;
			splineMeshBuilder.quality = 12;
			gameObject.AddComponent<MaterialReturnOnDestroy>().ReturnMaterial = delegate(Material material)
			{
				_materials.Enqueue(material);
			};
			gameObject.SetActive(value: true);
			_splines[segment.id] = splineMeshBuilder;
		}
		static Color Grayscale(float p)
		{
			return new Color(p, p, p, 1f);
		}
	}

	private void GetSwitchAdjustments(TrackSegment segment, Transform parent, ref Vector3 pointOffsetA, ref Vector3 pointOffsetB, ref float thresholdA, ref float thresholdB)
	{
		if (CheckForSwitch(segment, out var thrownAgainstA, out var thrownAgainstB, out var switchNodeA, out var switchNodeB))
		{
			bool num = switchNodeA != null && switchNodeB != null;
			float length = segment.GetLength();
			float num2 = (num ? (0.5f - 2.5f / length) : Mathf.InverseLerp(0f, length, 40f));
			if (thrownAgainstA)
			{
				thresholdA = num2;
			}
			if (thrownAgainstB)
			{
				thresholdB = 1f - num2;
			}
			if (thrownAgainstA)
			{
				pointOffsetA *= 4f;
			}
			if (thrownAgainstB)
			{
				pointOffsetB *= 4f;
			}
			AddSwitchStand(switchNodeA, segment, parent);
			AddSwitchStand(switchNodeB, segment, parent);
		}
	}

	private void AddSwitchStand(TrackNode node, TrackSegment trackSegment, Transform parent)
	{
		if (!(node == null) && (!_switchStands.TryGetValue(node.id, out var value) || !(value != null)))
		{
			MapSwitchStand mapSwitchStand = UnityEngine.Object.Instantiate(mapSwitchStandPrefab, parent);
			BezierCurve curve = trackSegment.Curve;
			trackSegment.GetPositionRotationAtDistance(5f, trackSegment.EndForNode(node), PositionAccuracy.Standard, out var position, out var _);
			mapSwitchStand.transform.localPosition = position - curve.P0;
			mapSwitchStand.transform.localRotation = Quaternion.LookRotation(position - node.transform.position.WorldToGame());
			mapSwitchStand.node = node;
			_switchStands[node.id] = mapSwitchStand;
			mapSwitchStand.gameObject.SetActive(ShowSwitchStands);
		}
	}

	private static bool CheckForSwitch(TrackSegment segment, out bool thrownAgainstA, out bool thrownAgainstB, out TrackNode switchNodeA, out TrackNode switchNodeB)
	{
		Graph graph = Graph.Shared;
		bool num = CheckEnd(TrackSegment.End.A, out switchNodeA, out thrownAgainstA);
		bool flag = CheckEnd(TrackSegment.End.B, out switchNodeB, out thrownAgainstB);
		return num || flag;
		bool CheckEnd(TrackSegment.End end, out TrackNode switchNode, out bool isAgainst)
		{
			TrackNode trackNode = segment.NodeForEnd(end);
			if (!graph.DecodeSwitchAt(trackNode, out var enter, out var a, out var b) || enter == segment)
			{
				isAgainst = false;
				switchNode = null;
				return false;
			}
			switchNode = trackNode;
			isAgainst = (segment == a && trackNode.isThrown) || (segment == b && !trackNode.isThrown);
			return true;
		}
	}

	private Material MaterialForSegment(TrackSegment segment, float thresholdA, float thresholdB)
	{
		if (!_materials.TryDequeue(out var result))
		{
			result = new Material(splineMaterial);
		}
		result.color = ColorForSegment(segment);
		result.SetColor("_AltColor", TrackColorUnavailable);
		result.SetFloat("_ThresholdA", thresholdA);
		result.SetFloat("_ThresholdB", thresholdB);
		return result;
	}

	private void TrimMaterialPool(int max = 32)
	{
		while (_materials.Count > max)
		{
			UnityEngine.Object.Destroy(_materials.Dequeue());
		}
	}

	private Color ColorForSegment(TrackSegment segment)
	{
		Color result = segment.trackClass switch
		{
			TrackClass.Mainline => TrackColorMainline, 
			TrackClass.Branch => TrackColorBranch, 
			TrackClass.Industrial => TrackColorIndustrial, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
		if (!segment.Available)
		{
			result = TrackColorUnavailable;
		}
		return result;
	}

	private void DestroyAllSplines()
	{
		while (_splines.Any())
		{
			KeyValuePair<string, SplineMeshBuilder> keyValuePair = _splines.Last();
			_splines.Remove(keyValuePair.Key);
			SplineMeshBuilder value = keyValuePair.Value;
			DestroySpline(value);
		}
		_switchStands.Clear();
	}

	private void CreateContainerIfNeeded()
	{
		if (!(_container != null))
		{
			GameObject gameObject = new GameObject("Container")
			{
				hideFlags = HideFlags.DontSave
			};
			_container = gameObject.transform;
			_container.SetParent(base.transform, worldPositionStays: false);
		}
	}

	private void DestroySpline(SplineMeshBuilder splineMeshBuilder)
	{
		UnityEngine.Object.Destroy(splineMeshBuilder.gameObject);
	}

	private void CullingGroupStateChanged(CullingGroupEvent sphere)
	{
		int num = sphere.index / 4;
		byte b = (byte)(sphere.index - num * 4);
		Entry entry = _entries[num];
		bool num2 = entry.Visibility != 0;
		if (sphere.isVisible && !sphere.wasVisible)
		{
			entry.Visibility = (byte)(entry.Visibility | (1 << (int)b));
		}
		else if (!sphere.isVisible && sphere.wasVisible)
		{
			entry.Visibility = (byte)(entry.Visibility & ~(1 << (int)b));
		}
		bool flag = entry.Visibility != 0;
		if (num2 != flag)
		{
			TrackSegment segment = entry.Segment;
			if (flag)
			{
				BuildSpline(segment);
				return;
			}
			SplineMeshBuilder splineMeshBuilder = _splines[segment.id];
			DestroySpline(splineMeshBuilder);
			_splines.Remove(segment.id);
		}
	}

	private void SwitchThrownDidChange(TrackNode node)
	{
		foreach (TrackSegment item in Graph.Shared.SegmentsConnectedTo(node))
		{
			if (_splines.TryGetValue(item.id, out var value))
			{
				UnityEngine.Object.DestroyImmediate(value.gameObject);
				_splines.Remove(item.id);
				BuildSpline(item);
			}
		}
	}
}
