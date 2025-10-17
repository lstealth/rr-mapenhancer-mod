using System;
using System.Collections.Generic;
using System.Threading;
using AssetPack.Runtime;
using KeyValue.Runtime;
using Model;
using Model.Database;
using Model.Definition;
using Model.Definition.Data;
using Model.Ops;
using Model.Ops.Definition;
using RollingStock.Controls;
using Serilog;
using UnityEngine;

namespace RollingStock;

public class CarLoadModelController : MonoBehaviour
{
	private KeyValueObject _keyValueObject;

	private IDisposable _observer;

	private int _slot;

	private string _loadIdentifier;

	private Load _load;

	private Car _car;

	private List<PositionRotationScale> _instancePositions;

	private readonly List<GameObject> _instanceGameObjects = new List<GameObject>();

	private CancellationTokenSource _modelLoadCancellationTokenSource;

	private readonly List<LoadedAssetReference<GameObject>> _modelLoadReferences = new List<LoadedAssetReference<GameObject>>();

	private string LoadKey => CarExtensions.KeyForLoadInfoSlot(_slot);

	private void OnEnable()
	{
		_keyValueObject = GetComponentInParent<KeyValueObject>();
		StartObserving();
	}

	private void OnDisable()
	{
		_observer?.Dispose();
		_observer = null;
		_modelLoadCancellationTokenSource?.Cancel();
		foreach (LoadedAssetReference<GameObject> modelLoadReference in _modelLoadReferences)
		{
			modelLoadReference.Dispose();
		}
		_modelLoadReferences.Clear();
	}

	public async void Configure(int slotIndex, string loadIdentifier, List<AssetReference> modelAssetReferences, List<PositionRotationScale> instancePositions)
	{
		_slot = slotIndex;
		_loadIdentifier = loadIdentifier;
		_instancePositions = instancePositions;
		_modelLoadCancellationTokenSource = new CancellationTokenSource();
		CancellationToken cancellationToken = _modelLoadCancellationTokenSource.Token;
		IPrefabStore prefabStore = TrainController.Shared.PrefabStore;
		_modelLoadReferences.Clear();
		foreach (AssetReference modelAssetReference in modelAssetReferences)
		{
			LoadedAssetReference<GameObject> item = await prefabStore.LoadAssetAsync<GameObject>(modelAssetReference.AssetPackIdentifier, modelAssetReference.AssetIdentifier, cancellationToken);
			if (this == null)
			{
				Log.Warning("CarLoadModelController destroyed while loading model.");
				return;
			}
			_modelLoadReferences.Add(item);
		}
		StartObserving();
	}

	private void StartObserving()
	{
		_observer?.Dispose();
		if (!(_keyValueObject == null))
		{
			_observer = _keyValueObject.Observe(LoadKey, delegate(Value value)
			{
				LoadChanged(CarLoadInfo.FromPropertyValue(value));
			});
		}
	}

	private bool LoadMatches(string loadId)
	{
		if (!(_loadIdentifier == "*"))
		{
			return _loadIdentifier == loadId;
		}
		return true;
	}

	private void LoadChanged(CarLoadInfo? carLoadInfo)
	{
		string loadId = carLoadInfo?.LoadId;
		if (!LoadMatches(loadId))
		{
			foreach (GameObject instanceGameObject in _instanceGameObjects)
			{
				UnityEngine.Object.Destroy(instanceGameObject);
			}
			_instanceGameObjects.Clear();
		}
		else if (_modelLoadReferences.Count > 0)
		{
			int num = TargetNumModels(carLoadInfo);
			while (_instanceGameObjects.Count > num)
			{
				List<GameObject> instanceGameObjects = _instanceGameObjects;
				GameObject gameObject = instanceGameObjects[instanceGameObjects.Count - 1];
				UnityEngine.Object.Destroy(gameObject);
				_instanceGameObjects.Remove(gameObject);
			}
			while (_instanceGameObjects.Count < num)
			{
				int count = _instanceGameObjects.Count;
				int index = count % _modelLoadReferences.Count;
				GameObject gameObject2 = UnityEngine.Object.Instantiate(_modelLoadReferences[index].Asset, base.transform, worldPositionStays: false);
				PositionRotationScale positionRotationScale = _instancePositions[count];
				gameObject2.transform.localPosition = positionRotationScale.Position;
				gameObject2.transform.localRotation = positionRotationScale.Rotation;
				gameObject2.transform.localScale = positionRotationScale.Scale;
				_instanceGameObjects.Add(gameObject2);
			}
		}
	}

	private int TargetNumModels(CarLoadInfo? carLoadInfo)
	{
		if (string.IsNullOrEmpty(_loadIdentifier))
		{
			return _instancePositions.Count;
		}
		if (!carLoadInfo.HasValue)
		{
			return 0;
		}
		CarLoadInfo value = carLoadInfo.Value;
		string loadId = value.LoadId;
		if (_load != null && loadId != _load.id)
		{
			_load = null;
		}
		if ((object)_load == null)
		{
			_load = CarPrototypeLibrary.instance.LoadForId(loadId);
		}
		if ((object)_car == null)
		{
			_car = GetComponentInParent<Car>();
		}
		float num = ((_load.units != LoadUnits.Quantity) ? (CarLoadAnimator.CalculatePercent(_car, value) * (float)_instancePositions.Count) : value.Quantity);
		int num2 = Mathf.CeilToInt(num);
		int num3 = ((!((float)num2 - num < 0.01f)) ? Mathf.FloorToInt(num) : num2);
		if (value.Quantity > 0.1f)
		{
			num3 = Mathf.Max(1, num3);
		}
		return num3;
	}
}
