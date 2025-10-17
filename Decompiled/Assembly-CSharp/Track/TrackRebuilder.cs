using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Helpers;
using Serilog;
using UnityEngine;
using UnityEngine.Profiling;

namespace Track;

public class TrackRebuilder : MonoBehaviour, ITrackRebuilder
{
	private class Entry
	{
		public readonly TrackObjectManager.ITrackDescriptor descriptor;

		public bool isInRange;

		public bool isVisible;

		public GameObject gameObject;

		public GameObject maskObject;

		public Entry(TrackObjectManager.ITrackDescriptor descriptor)
		{
			this.descriptor = descriptor;
		}
	}

	private CullingGroup _group;

	private readonly List<Entry> _destroyQueue = new List<Entry>();

	private readonly List<Entry> _buildQueue = new List<Entry>();

	private const int InitialSphereCount = 500;

	private BoundingSphere[] _spheres = new BoundingSphere[500];

	private Entry[] _sphereEntries = new Entry[500];

	private int _sphereCount;

	private readonly float[] _distanceBands = new float[2] { 1200f, 1800f };

	private Dictionary<string, Entry> _entryLookup = new Dictionary<string, Entry>();

	private Vector3 _lastOffset = Vector3.zero;

	private float _lastReport;

	public Func<TrackObjectManager.ITrackDescriptor, GameObject> BuildGameObject { get; set; }

	public Func<TrackObjectManager.ITrackDescriptor, GameObject> BuildMaskObject { get; set; }

	private void Start()
	{
		PrepareGroupIfNeeded();
	}

	private void OnDestroy()
	{
		if (_group != null)
		{
			CullingGroup cullingGroup = _group;
			cullingGroup.onStateChanged = (CullingGroup.StateChanged)Delegate.Remove(cullingGroup.onStateChanged, new CullingGroup.StateChanged(StateChanged));
			_group.Dispose();
			_group = null;
		}
	}

	private void Update()
	{
		if (base.transform.hasChanged)
		{
			Vector3 vector = base.transform.position - _lastOffset;
			Log.Debug("TrackRebuilder: Updating culling group {offset}", vector);
			for (int i = 0; i < _sphereCount; i++)
			{
				_spheres[i].position += vector;
			}
			_lastOffset = base.transform.position;
			_group.SetBoundingSpheres(_spheres);
			_group.SetBoundingSphereCount(_sphereCount);
			base.transform.hasChanged = false;
		}
		WorkBuildQueue();
		WorkDestroyQueue();
	}

	private void WorkBuildQueue()
	{
		if (_buildQueue.Any())
		{
			float time = Time.time;
			while (time + 1f / 60f > Time.time && _buildQueue.Any())
			{
				Entry entry = _buildQueue[0];
				_buildQueue.RemoveAt(0);
				entry.gameObject = BuildGameObject(entry.descriptor);
			}
		}
	}

	private void PrepareGroupIfNeeded()
	{
		if (_group == null)
		{
			Camera main = Camera.main;
			_group = new CullingGroup();
			_group.targetCamera = main;
			_group.SetDistanceReferencePoint(main.transform);
			_group.SetBoundingSpheres(_spheres);
			_group.SetBoundingSphereCount(_sphereCount);
			_group.SetBoundingDistances(_distanceBands);
			CullingGroup cullingGroup = _group;
			cullingGroup.onStateChanged = (CullingGroup.StateChanged)Delegate.Combine(cullingGroup.onStateChanged, new CullingGroup.StateChanged(StateChanged));
		}
	}

	public void Add(TrackObjectManager.ITrackDescriptor descriptor, BezierCurve curve)
	{
		Entry entry = new Entry(descriptor);
		_entryLookup[descriptor.Identifier] = entry;
		Add(curve, entry);
		entry.maskObject = BuildMaskObject(descriptor);
		SetInitialVisibility(entry);
	}

	public void Add(TrackObjectManager.ITrackDescriptor descriptor, BoundingSphere boundingSphere)
	{
		Entry entry = new Entry(descriptor);
		_entryLookup[descriptor.Identifier] = entry;
		AddSphere(boundingSphere.position, boundingSphere.radius, entry);
		entry.maskObject = BuildMaskObject(descriptor);
		SetInitialVisibility(entry);
	}

	public void Remove(TrackObjectManager.ITrackDescriptor descriptor)
	{
		if (!_entryLookup.TryGetValue(descriptor.Identifier, out var value))
		{
			Debug.LogWarning($"Remove({descriptor}) failed -- there is no entry");
			return;
		}
		_entryLookup.Remove(descriptor.Identifier);
		UnityEngine.Object.DestroyImmediate(value.maskObject);
		for (int i = 0; i < _sphereCount; i++)
		{
			if (_sphereEntries[i] == value)
			{
				_group.EraseSwapBack(i);
				CullingGroup.EraseSwapBack(i, _sphereEntries, ref _sphereCount);
				i--;
			}
		}
		_destroyQueue.Add(value);
	}

	private void Add(BezierCurve curve, Entry entry)
	{
		LineCurve lineCurve = new LineCurve(curve.Approximate(1.0001f), Hand.Left);
		while (!lineCurve.IsEmpty)
		{
			AddSphere(lineCurve.Head.point, 50f, entry);
			lineCurve = lineCurve.Skip(75f, failSilently: true);
		}
		AddSphere(lineCurve.Tail.point, 50f, entry);
	}

	private void AddSphere(Vector3 position, float radius, Entry entry)
	{
		PrepareGroupIfNeeded();
		if (_sphereCount == _spheres.Length)
		{
			Array.Resize(ref _spheres, _sphereCount * 3 / 2 + 1);
			Array.Resize(ref _sphereEntries, _spheres.Length);
			_group.SetBoundingSpheres(_spheres);
		}
		_spheres[_sphereCount].position = WorldTransformer.GameToWorld(position);
		_spheres[_sphereCount].radius = radius;
		_sphereEntries[_sphereCount] = entry;
		_sphereCount++;
		_group.SetBoundingSphereCount(_sphereCount);
	}

	public void Clear()
	{
		_sphereCount = 0;
		_group?.SetBoundingSphereCount(0);
		_buildQueue.Clear();
		_destroyQueue.Clear();
		_entryLookup.Clear();
	}

	private void SetInitialVisibility(Entry entry)
	{
		PrepareGroupIfNeeded();
		int num = _distanceBands.Length;
		for (int i = 0; i < _sphereCount; i++)
		{
			if (_sphereEntries[i] == entry)
			{
				num = Mathf.Min(num, _group.CalculateDistanceBand(_spheres[i].position, _distanceBands));
			}
		}
		entry.isInRange = num <= 0;
		InRangeDidChange(entry);
	}

	private void StateChanged(CullingGroupEvent evt)
	{
		Entry entry = _sphereEntries[evt.index];
		bool flag = false;
		int num = _distanceBands.Length;
		for (int i = 0; i < _sphereCount; i++)
		{
			if (_sphereEntries[i] == entry)
			{
				flag = flag || _group.IsVisible(i);
				num = Mathf.Min(num, _group.GetDistance(i));
			}
		}
		bool flag2 = entry.isInRange;
		if (num > 1)
		{
			flag2 = false;
		}
		else if (num <= 0)
		{
			flag2 = true;
		}
		if (entry.isInRange != flag2)
		{
			entry.isInRange = flag2;
			InRangeDidChange(entry);
		}
		if (entry.isVisible != flag)
		{
			entry.isVisible = flag;
			IsVisibleDidChange(entry);
		}
	}

	private void InRangeDidChange(Entry entry)
	{
		if (entry.isInRange)
		{
			if (entry.gameObject != null)
			{
				_destroyQueue.Remove(entry);
				entry.gameObject.SetActive(value: true);
			}
			else if (!_buildQueue.Contains(entry))
			{
				_buildQueue.Add(entry);
			}
		}
		else if (entry.gameObject != null)
		{
			entry.gameObject.SetActive(value: false);
			_destroyQueue.Add(entry);
		}
	}

	private void IsVisibleDidChange(Entry entry)
	{
		if (!(entry.gameObject == null))
		{
			entry.gameObject.SetActive(entry.isVisible && entry.isInRange);
		}
	}

	private void WorkDestroyQueue()
	{
		while (_destroyQueue.Count > 0)
		{
			Entry entry = _destroyQueue[0];
			_destroyQueue.RemoveAt(0);
			UnityEngine.Object.Destroy(entry.gameObject);
			entry.gameObject = null;
		}
	}

	private void ShowMeshMemoryUsage()
	{
		if (!(_lastReport + 5f < Time.time))
		{
			return;
		}
		HashSet<Mesh> hashSet = new HashSet<Mesh>();
		foreach (Entry item in _sphereEntries.ToHashSet())
		{
			if ((bool)item.gameObject && item.gameObject.activeInHierarchy)
			{
				MeshFilter[] componentsInChildren = item.gameObject.GetComponentsInChildren<MeshFilter>();
				foreach (MeshFilter meshFilter in componentsInChildren)
				{
					hashSet.Add(meshFilter.sharedMesh);
				}
			}
		}
		Dictionary<string, long> dictionary = new Dictionary<string, long>();
		foreach (Mesh item2 in hashSet)
		{
			string key = item2.name;
			long num = (dictionary.ContainsKey(key) ? dictionary[key] : 0);
			num += Profiler.GetRuntimeMemorySizeLong(item2);
			dictionary[key] = num;
		}
		IEnumerable<string> values = dictionary.Select((KeyValuePair<string, long> kv) => $"{kv.Key}: {kv.Value / 1000:N0}KB");
		Debug.Log("Totals: " + string.Join(", ", values));
		_lastReport = Time.time;
	}
}
