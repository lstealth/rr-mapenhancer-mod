using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AssetPack.Runtime;
using KeyValue.Runtime;
using Model;
using Model.Database;
using Model.Definition;
using Model.Definition.Components;
using Model.Definition.Data;
using Model.Ops;
using Model.Ops.Definition;
using Serilog;
using UnityEngine;

namespace RollingStock.LoadModels;

public class AggregateLoadModelController : MonoBehaviour
{
	[Range(0f, 7f)]
	public int atlasIndex;

	private KeyValueObject _keyValueObject;

	private IDisposable _observer;

	private int _slot;

	private string _loadIdentifier;

	private Load _load;

	private Car _car;

	private Vector3 _maxSize;

	private List<AggregateLoadModelComponent.Keyframe> _keyframes;

	private string _currentLoadId;

	private CancellationTokenSource _materialLoadCancellationTokenSource;

	private LoadedAssetReference<Material> _loadReference;

	private GameObject _meshGameObject;

	private MaterialDefinition _materialDefinition;

	private string _warnedMissingMaterialDefinitionForLoadId;

	private static readonly float[] LODTransitionHeights = new float[2] { 0.8f, 0.025f };

	private const string MaterialFieldAggregateModelLoadId = "aggregateModelLoadId";

	private const string MaterialFieldAggregateModelBumps = "aggregateModelBumps";

	private string LoadKey => CarExtensions.KeyForLoadInfoSlot(_slot);

	private int IdHashCode
	{
		get
		{
			if (!(_car == null))
			{
				return Mathf.Abs(_car.id.GetHashCode());
			}
			return 0;
		}
	}

	private void OnEnable()
	{
		_keyValueObject = GetComponentInParent<KeyValueObject>();
		StartObserving();
	}

	private void OnDisable()
	{
		_materialLoadCancellationTokenSource?.Cancel();
		_materialLoadCancellationTokenSource = null;
		_loadReference?.Dispose();
		_loadReference = null;
		_observer?.Dispose();
		_observer = null;
		_currentLoadId = null;
		DestroyMeshGameObject();
	}

	public void Configure(List<AggregateLoadModelComponent.Keyframe> keyframes)
	{
		_slot = 0;
		_keyframes = keyframes.ToList();
		_keyframes.Sort((AggregateLoadModelComponent.Keyframe x, AggregateLoadModelComponent.Keyframe y) => x.PercentFull.CompareTo(y.PercentFull));
		_maxSize = Vector2.zero;
		foreach (AggregateLoadModelComponent.Keyframe keyframe in _keyframes)
		{
			if (_maxSize.sqrMagnitude < keyframe.Size.sqrMagnitude)
			{
				_maxSize = keyframe.Size;
			}
		}
		if (_maxSize.x == 0f)
		{
			_maxSize.x = 0.001f;
		}
		if (_maxSize.y == 0f)
		{
			_maxSize.y = 0.001f;
		}
		if (_maxSize.z == 0f)
		{
			_maxSize.z = 0.001f;
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

	private async void LoadChanged(CarLoadInfo? carLoadInfo)
	{
		if ((object)_car == null)
		{
			_car = GetComponentInParent<Car>();
		}
		string text = carLoadInfo?.LoadId;
		if (_currentLoadId != text)
		{
			_loadReference?.Dispose();
			_loadReference = null;
			DestroyMeshGameObject();
		}
		_currentLoadId = text;
		if (string.IsNullOrEmpty(_currentLoadId) || !carLoadInfo.HasValue)
		{
			return;
		}
		CarLoadInfo info = carLoadInfo.GetValueOrDefault();
		bool createMesh = _meshGameObject == null;
		if (createMesh)
		{
			IPrefabStore prefabStore = TrainController.Shared.PrefabStore;
			if (!TryGetMaterialDefinition(prefabStore, out var materialDefinitionItem))
			{
				return;
			}
			_materialLoadCancellationTokenSource?.Cancel();
			_materialLoadCancellationTokenSource = new CancellationTokenSource();
			CancellationToken token = _materialLoadCancellationTokenSource.Token;
			string materialAssetIdentifier = (_materialDefinition = materialDefinitionItem.Definition).AssetIdentifier;
			string materialAssetPackIdentifier = prefabStore.AssetPackIdentifierContainingDefinition(materialAssetIdentifier);
			UpdateAtlasIndex();
			try
			{
				_loadReference = await prefabStore.LoadAssetAsync<Material>(materialAssetPackIdentifier, materialAssetIdentifier, token);
			}
			catch (OperationCanceledException)
			{
				return;
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Unable to load asset {pack} {assetId}", materialAssetPackIdentifier, materialAssetIdentifier);
				return;
			}
			finally
			{
				_materialLoadCancellationTokenSource = null;
			}
			_meshGameObject = new GameObject("Aggregate Mesh")
			{
				hideFlags = HideFlags.DontSave
			};
			_meshGameObject.transform.SetParent(base.transform);
			_meshGameObject.transform.localRotation = Quaternion.Euler(0f, IdHashCode % 2 * 180, 0f);
			LODGroup lODGroup = _meshGameObject.AddComponent<LODGroup>();
			int num = LODTransitionHeights.Length;
			LOD[] array = new LOD[num];
			Material material = new Material(_loadReference.Asset);
			float num2 = Mathf.Max(0.001f, _materialDefinition.EdgeLength);
			material.SetVector("_Tiling", new Vector2(_maxSize.x / num2, _maxSize.z / num2));
			for (int i = 0; i < num; i++)
			{
				GameObject obj = new GameObject($"LOD{i}");
				obj.transform.SetParent(_meshGameObject.transform, worldPositionStays: false);
				MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
				meshFilter.sharedMesh = AggregateLoadModelPool.Shared.GetMesh(atlasIndex, new Vector2(_maxSize.x, _maxSize.z), i);
				MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
				meshRenderer.sharedMaterial = material;
				float screenRelativeTransitionHeight = LODTransitionHeights[i];
				array[i] = new LOD(screenRelativeTransitionHeight, new Renderer[1] { meshRenderer });
				if (i + 1 == num)
				{
					_meshGameObject.AddComponent<MeshCollider>().sharedMesh = meshFilter.sharedMesh;
				}
			}
			lODGroup.SetLODs(array);
			lODGroup.RecalculateBounds();
		}
		float currentPercent = GetCurrentPercent(info);
		var (vector, vector2) = InterpolatedKeyframe(currentPercent);
		if (createMesh)
		{
			_meshGameObject.transform.localPosition = vector;
			_meshGameObject.transform.localScale = vector2;
		}
		else
		{
			LeanTween.cancel(_meshGameObject);
			LeanTween.moveLocal(_meshGameObject, vector, 1f).setEaseOutCubic();
			LeanTween.scale(_meshGameObject, vector2, 1f).setEaseOutCubic();
		}
	}

	private void UpdateAtlasIndex()
	{
		if (TryGetField(_materialDefinition, "aggregateModelBumps", out var value))
		{
			string[] array = value.Split(",");
			List<int> list = new List<int>();
			string[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				if (int.TryParse(array2[i], out var result))
				{
					list.Add(result);
				}
			}
			if (list.Count > 0)
			{
				int num = list[IdHashCode % list.Count];
				if (num >= 1 && num <= 4)
				{
					atlasIndex = (num - 1) * 2 + IdHashCode % 2;
					return;
				}
			}
		}
		atlasIndex = (IdHashCode + 17) % 8;
	}

	private void DestroyMeshGameObject()
	{
		if (!(_meshGameObject == null))
		{
			if (_meshGameObject.TryGetComponent<MeshRenderer>(out var component) && component.sharedMaterial != null)
			{
				UnityEngine.Object.Destroy(component.sharedMaterial);
			}
			UnityEngine.Object.Destroy(_meshGameObject);
			_meshGameObject = null;
		}
	}

	private bool TryGetMaterialDefinition(IPrefabStore prefabStore, out TypedContainerItem<MaterialDefinition> materialDefinitionItem)
	{
		IEnumerable<TypedContainerItem<MaterialDefinition>> enumerable = prefabStore.AllDefinitionInfosOfType<MaterialDefinition>();
		materialDefinitionItem = null;
		foreach (TypedContainerItem<MaterialDefinition> item in enumerable)
		{
			if (Matches(item, _currentLoadId))
			{
				materialDefinitionItem = item;
				break;
			}
		}
		if (materialDefinitionItem == null)
		{
			if (_warnedMissingMaterialDefinitionForLoadId != _currentLoadId)
			{
				Log.Warning("Couldn't find material definition for load {loadId}", _currentLoadId);
				_warnedMissingMaterialDefinitionForLoadId = _currentLoadId;
			}
			return false;
		}
		return true;
	}

	private float GetCurrentPercent(CarLoadInfo info)
	{
		foreach (LoadSlot loadSlot in _car.Definition.LoadSlots)
		{
			if (loadSlot.LoadRequirementsMatch(info.LoadId))
			{
				return Mathf.Clamp01(info.Quantity / loadSlot.MaximumCapacity);
			}
		}
		return 0f;
	}

	private (Vector3 position, Vector3 size) InterpolatedKeyframe(float percent)
	{
		for (int i = 0; i < _keyframes.Count - 1; i++)
		{
			AggregateLoadModelComponent.Keyframe keyframe = _keyframes[i];
			AggregateLoadModelComponent.Keyframe keyframe2 = _keyframes[i + 1];
			if (keyframe.PercentFull <= percent && percent <= keyframe2.PercentFull)
			{
				float t = Mathf.InverseLerp(keyframe.PercentFull, keyframe2.PercentFull, percent);
				return (position: Vector3.Lerp(keyframe.Position, keyframe2.Position, t), size: Vector3.Lerp(keyframe.Size, keyframe2.Size, t));
			}
		}
		List<AggregateLoadModelComponent.Keyframe> keyframes = _keyframes;
		Vector3 position = keyframes[keyframes.Count - 1].Position;
		List<AggregateLoadModelComponent.Keyframe> keyframes2 = _keyframes;
		return (position: position, size: keyframes2[keyframes2.Count - 1].Size);
	}

	private static bool Matches(TypedContainerItem<MaterialDefinition> item, string loadId)
	{
		if (TryGetField(item.Definition, "aggregateModelLoadId", out var value))
		{
			return value == loadId;
		}
		return false;
	}

	private static bool TryGetField(MaterialDefinition definition, string key, out string value)
	{
		foreach (MaterialDefinition.FieldPair field in definition.Fields)
		{
			if (field.Key == key)
			{
				value = field.Value;
				return true;
			}
		}
		value = null;
		return false;
	}
}
