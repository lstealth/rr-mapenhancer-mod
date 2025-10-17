using System;
using System.Collections.Generic;
using Helpers;
using Model;
using UnityEngine;

namespace RollingStock;

public class CarCuller : MonoBehaviour
{
	private enum Action
	{
		Load,
		Unload
	}

	private class Record
	{
		public readonly Car Car;

		public IDisposable LoadToken;

		public Record(Car car)
		{
			Car = car;
		}
	}

	public TrainController trainController;

	private CullingGroup _cullingGroup;

	private BoundingSphere[] _spheres;

	private List<Record> _records;

	private readonly Dictionary<string, int> _cachedSphereIndexes = new Dictionary<string, int>();

	public const int DistanceBandClose = 0;

	public const int DistanceBandNearby = 1;

	public const int DistanceBandLoadModel = 2;

	public const int DistanceBandNoChange = 3;

	public const int DistanceBandUnloadModel = 4;

	private readonly float[] _distanceBands = new float[4] { 50f, 100f, 1500f, 1750f };

	private readonly Dictionary<Record, Action> _pending = new Dictionary<Record, Action>();

	private void Awake()
	{
		SetupCarCullingGroup();
	}

	private void OnDestroy()
	{
		TeardownCarCullingGroup();
	}

	private void Update()
	{
		ProcessPending();
	}

	public void Add(Car car)
	{
		int count = _records.Count;
		Record item = new Record(car);
		_records.Add(item);
		if (_records.Count > _spheres.Length)
		{
			BoundingSphere[] array = new BoundingSphere[Mathf.CeilToInt((float)_spheres.Length * 1.5f)];
			Array.Copy(_spheres, array, _spheres.Length);
			_spheres = array;
			_cullingGroup.SetBoundingSpheres(_spheres);
		}
		_spheres[count].radius = car.carLength;
		_cullingGroup.SetBoundingSphereCount(_records.Count);
	}

	public void PostAdd(Car car)
	{
		if (TryGetSphereIndexOfCar(car, out var index))
		{
			CarDidMove(car, GetSpherePosition(car));
			if (_records[index].LoadToken == null && _cullingGroup.CalculateDistanceBand(_spheres[index].position, _distanceBands) < 1)
			{
				_records[index].LoadToken = car.ModelLoadRetain("CarCuller");
			}
		}
	}

	public void Remove(Car car)
	{
		if (TryGetSphereIndexOfCar(car, out var index))
		{
			_pending.Remove(_records[index]);
			_records[index].LoadToken?.Dispose();
			_records[index].LoadToken = null;
			_records.RemoveAt(index);
			Array.Copy(_spheres, index + 1, _spheres, index, _spheres.Length - index - 1);
			_cullingGroup.SetBoundingSphereCount(_records.Count);
			_cachedSphereIndexes.Clear();
		}
	}

	public void CarDidMove(Car car, Vector3 worldPosition)
	{
		if (TryGetSphereIndexOfCar(car, out var index))
		{
			_spheres[index].position = worldPosition;
		}
	}

	private bool TryGetSphereIndexOfCar(Car car, out int index)
	{
		if (_cachedSphereIndexes.TryGetValue(car.id, out index))
		{
			return true;
		}
		for (index = 0; index < _records.Count; index++)
		{
			if (_records[index].Car == car)
			{
				_cachedSphereIndexes[car.id] = index;
				return true;
			}
		}
		return false;
	}

	private void SetupCarCullingGroup()
	{
		_spheres = new BoundingSphere[64];
		_records = new List<Record>(64);
		_cullingGroup = new CullingGroup();
		_cullingGroup.SetBoundingSpheres(_spheres);
		_cullingGroup.SetBoundingSphereCount(0);
		_cullingGroup.SetBoundingDistances(_distanceBands);
		_cullingGroup.onStateChanged = OnCarCullingGroupStateChanged;
		Camera main = Camera.main;
		_cullingGroup.targetCamera = main;
		_cullingGroup.SetDistanceReferencePoint(main.transform);
	}

	private void TeardownCarCullingGroup()
	{
		_cullingGroup?.Dispose();
		_cullingGroup = null;
	}

	private void ProcessPending()
	{
		foreach (var (record2, action2) in _pending)
		{
			switch (action2)
			{
			case Action.Load:
				if (record2.LoadToken == null)
				{
					record2.LoadToken = record2.Car.ModelLoadRetain("CarCuller");
				}
				break;
			case Action.Unload:
				if (record2.LoadToken != null)
				{
					record2.LoadToken.Dispose();
					record2.LoadToken = null;
				}
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}
		_pending.Clear();
	}

	private void OnCarCullingGroupStateChanged(CullingGroupEvent sphere)
	{
		Record record = _records[sphere.index];
		Car car = record.Car;
		if (car == null)
		{
			return;
		}
		int currentDistance = sphere.currentDistance;
		if (currentDistance > 2)
		{
			if (currentDistance >= 4)
			{
				_pending[record] = Action.Unload;
			}
		}
		else
		{
			_pending[record] = Action.Load;
		}
		car.SetCullerDistanceBand(sphere.previousDistance, sphere.currentDistance);
		if (sphere.hasBecomeVisible || sphere.hasBecomeInvisible)
		{
			car.SetVisible(sphere.isVisible);
		}
	}

	public void WorldDidMove(Vector3 offset)
	{
		for (int i = 0; i < _records.Count; i++)
		{
			Record record = _records[i];
			_spheres[i].position = GetSpherePosition(record.Car);
		}
	}

	private Vector3 GetSpherePosition(Car car)
	{
		return WorldTransformer.GameToWorld(car.GetCenterPosition(trainController.graph));
	}
}
