using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AssetPack.Common;
using AssetPack.Runtime;
using Model.Definition;
using Model.Definition.Data;
using RollingStock;
using Serilog;
using UnityEngine;

namespace Model.Database;

public class PrefabStore : IDisposable, IPrefabStore
{
	public class UnknownIdentifierException : Exception
	{
		public string Identifier { get; }

		public UnknownIdentifierException(string identifier)
			: base("Unknown identifier: " + identifier)
		{
			Identifier = identifier;
		}
	}

	private readonly List<AssetPackRuntimeStore> _stores = new List<AssetPackRuntimeStore>();

	private readonly Dictionary<string, Task<Wheelset>> _truckPrefabTasks = new Dictionary<string, Task<Wheelset>>();

	private readonly HashSet<LoadedAssetReference<GameObject>> _truckReferences = new HashSet<LoadedAssetReference<GameObject>>();

	public IEnumerable<AssetPackRuntimeStore> ExternalStores => _stores.Where((AssetPackRuntimeStore store) => store.Location == AssetPackRuntimeStore.StoreLocation.External);

	public IEnumerable<TypedContainerItem<CarDefinition>> AllCarDefinitionInfos
	{
		get
		{
			HashSet<string> hashSet = new HashSet<string>();
			foreach (AssetPackRuntimeStore store in _stores)
			{
				foreach (ContainerItem @object in store.Container().Objects)
				{
					if (@object.Definition is CarDefinition)
					{
						hashSet.Add(@object.Identifier);
					}
				}
			}
			return hashSet.Select(CarDefinitionInfoForIdentifier);
		}
	}

	public static PrefabStore Create()
	{
		PrefabStore instance = new PrefabStore();
		AddStoresFromLocation(AssetPackRuntimeStore.StoreLocation.Internal);
		AddStoresFromLocation(AssetPackRuntimeStore.StoreLocation.External);
		instance.LoadAssetPackStatically("shared");
		instance.CheckDefinitions();
		return instance;
		void AddStoresFromLocation(AssetPackRuntimeStore.StoreLocation location)
		{
			foreach (string item in Utilities.FindAssetPacks(AssetPackRuntimeStore.BasePathForLocation(location)))
			{
				instance.AddStore(item, location);
			}
		}
	}

	private void LoadAssetPackStatically(string storeIdentifier)
	{
		AssetPackRuntimeStore assetPackRuntimeStore = _stores.FirstOrDefault((AssetPackRuntimeStore store) => store.Identifier == storeIdentifier);
		if (assetPackRuntimeStore == null)
		{
			Debug.LogWarning("Can't load asset pack statically; not found: " + storeIdentifier);
		}
		else
		{
			assetPackRuntimeStore.LoadBundleStatic();
		}
	}

	private void AddStore(string storeIdentifier, AssetPackRuntimeStore.StoreLocation location)
	{
		AssetPackRuntimeStore assetPackRuntimeStore = new AssetPackRuntimeStore(storeIdentifier, location);
		for (int i = 0; i < _stores.Count; i++)
		{
			AssetPackRuntimeStore assetPackRuntimeStore2 = _stores[i];
			if (assetPackRuntimeStore2.Identifier == storeIdentifier)
			{
				if (location == AssetPackRuntimeStore.StoreLocation.External)
				{
					Log.Information("Replace Store {location}: {identifier}", location, storeIdentifier);
				}
				assetPackRuntimeStore2.Dispose();
				_stores[i] = assetPackRuntimeStore;
				return;
			}
		}
		if (location == AssetPackRuntimeStore.StoreLocation.External)
		{
			Log.Information("Add Store {location}: {identifier}", location, storeIdentifier);
		}
		_stores.Add(assetPackRuntimeStore);
	}

	public void Dispose()
	{
		Debug.Log("PrefabStore Dispose");
		foreach (KeyValuePair<string, Task<Wheelset>> truckPrefabTask in _truckPrefabTasks)
		{
			truckPrefabTask.Deconstruct(out var _, out var value);
			value.Dispose();
		}
		_truckPrefabTasks.Clear();
		foreach (LoadedAssetReference<GameObject> truckReference in _truckReferences)
		{
			truckReference.Dispose();
		}
		_truckReferences.Clear();
		foreach (AssetPackRuntimeStore store in _stores)
		{
			store.Dispose();
		}
		_stores.Clear();
	}

	private AssetPackRuntimeStore AssetPackForIdentifier(string assetPackIdentifier)
	{
		for (int i = 0; i < _stores.Count; i++)
		{
			AssetPackRuntimeStore assetPackRuntimeStore = _stores[i];
			if (assetPackRuntimeStore.Identifier == assetPackIdentifier)
			{
				return assetPackRuntimeStore;
			}
		}
		throw new Exception("No store with identifier " + assetPackIdentifier);
	}

	private AssetPackRuntimeStore AssetPackContainingIdentifier(string identifier)
	{
		for (int i = 0; i < _stores.Count; i++)
		{
			AssetPackRuntimeStore assetPackRuntimeStore = _stores[i];
			if (assetPackRuntimeStore.ContainsIdentifier(identifier))
			{
				return assetPackRuntimeStore;
			}
		}
		throw new UnknownIdentifierException(identifier);
	}

	public Task<Wheelset> TruckPrefabForId(string truckIdentifier)
	{
		if (_truckPrefabTasks.TryGetValue(truckIdentifier, out var value))
		{
			return value;
		}
		TaskCompletionSource<Wheelset> taskCompletionSource = new TaskCompletionSource<Wheelset>();
		Task<Wheelset> task = taskCompletionSource.Task;
		_truckPrefabTasks[truckIdentifier] = task;
		LoadWheelset(truckIdentifier, taskCompletionSource);
		return task;
	}

	private async void LoadWheelset(string truckIdentifier, TaskCompletionSource<Wheelset> tcs)
	{
		try
		{
			AssetPackRuntimeStore assetPackRuntimeStore = AssetPackContainingIdentifier(truckIdentifier);
			ObjectMetadata metadata;
			TruckDefinition truckDefinition = DefinitionForIdentifier<TruckDefinition>(assetPackRuntimeStore, truckIdentifier, out metadata);
			LoadedAssetReference<GameObject> loadedAssetReference = await assetPackRuntimeStore.LoadAsset<GameObject>(truckDefinition.ModelIdentifier, CancellationToken.None);
			try
			{
				GameObject asset = loadedAssetReference.Asset;
				CarShaderHelper.Instance.ReplaceShaders(asset);
				Wheelset wheelset = asset.AddComponent<Wheelset>();
				AnimationMap componentInChildren = asset.GetComponentInChildren<AnimationMap>();
				Animator animator = componentInChildren.gameObject.AddComponent<Animator>();
				animator.cullingMode = AnimatorCullingMode.CullCompletely;
				wheelset.diameterInInches = truckDefinition.Diameter * 3.28084f * 12f;
				wheelset.animator = animator;
				wheelset.applyBrakesAnimationClip = componentInChildren.ClipForName(truckDefinition.BrakeAnimation.ClipName);
				foreach (TransformReference wheelTransform in truckDefinition.WheelTransforms)
				{
					try
					{
						wheelset.wheels.Add(wheelset.transform.ResolveTransform(wheelTransform));
					}
					catch (Exception exception)
					{
						Log.Error(exception, "Error resolving transform on truck wheel {truckIdentifier}, {transformReference}", truckIdentifier, wheelTransform);
						Debug.LogException(exception);
					}
				}
				tcs.SetResult(wheelset);
				_truckReferences.Add(loadedAssetReference);
			}
			catch (Exception exception2)
			{
				Log.Error(exception2, "Error preparing truck prefab {truckIdentifier}", truckIdentifier);
				Debug.LogException(exception2);
				loadedAssetReference.Dispose();
				throw;
			}
		}
		catch (Exception exception3)
		{
			tcs.SetException(exception3);
		}
	}

	public async Task<LoadedAssetReference<T>> LoadAssetAsync<T>(string assetPackIdentifier, string assetIdentifier, CancellationToken cancellationToken) where T : UnityEngine.Object
	{
		if (string.IsNullOrEmpty(assetPackIdentifier))
		{
			throw new ArgumentException("assetPackIdentifier required", "assetPackIdentifier");
		}
		if (string.IsNullOrEmpty(assetIdentifier))
		{
			throw new ArgumentException("assetIdentifier required", "assetIdentifier");
		}
		LoadedAssetReference<T> loadedAssetReference = await AssetPackForIdentifier(assetPackIdentifier).LoadAsset<T>(assetIdentifier, cancellationToken);
		if (CarShaderHelper.Instance != null)
		{
			CarShaderHelper.Instance.ReplaceShaders(loadedAssetReference.Asset);
		}
		return loadedAssetReference;
	}

	public string AssetPackIdentifierContainingDefinition(string definitionIdentifier)
	{
		return AssetPackContainingIdentifier(definitionIdentifier).Identifier;
	}

	private T DefinitionForIdentifier<T>(AssetPackRuntimeStore store, string identifier, out ObjectMetadata metadata)
	{
		ContainerItem containerItem = store.ContainerItemForObjectIdentifier(identifier);
		metadata = containerItem.Metadata;
		ObjectDefinition definition = containerItem.Definition;
		if (definition is T)
		{
			return (T)(object)((definition is T) ? definition : null);
		}
		throw new Exception($"Definition {identifier} does not describe {typeof(T)}, is {containerItem.Definition.Kind}");
	}

	private TypedContainerItem<TDefinition> DefinitionForIdentifier<TDefinition>(string identifier) where TDefinition : ObjectDefinition
	{
		ContainerItem containerItem = ContainerItemForIdentifier(identifier);
		return ((containerItem.Definition as TDefinition) ?? throw new Exception($"Definition {identifier} does not describe {typeof(TDefinition)}, is {containerItem.Definition.Kind}")).TypedContainerItem<TDefinition>(containerItem);
	}

	public TypedContainerItem<CarDefinition> CarDefinitionInfoForIdentifier(string identifier)
	{
		ContainerItem containerItem = ContainerItemForIdentifier(identifier);
		return ((containerItem.Definition as CarDefinition) ?? throw new Exception($"Definition {identifier} does not describe {typeof(CarDefinition)}, is {containerItem.Definition.Kind}")).TypedContainerItem<CarDefinition>(containerItem);
	}

	private ContainerItem ContainerItemForIdentifier(string identifier)
	{
		return AssetPackContainingIdentifier(identifier).ContainerItemForObjectIdentifier(identifier);
	}

	public T DefinitionForIdentifier<T>(string definitionIdentifier, out ObjectMetadata metadata)
	{
		AssetPackRuntimeStore store = AssetPackContainingIdentifier(definitionIdentifier);
		return DefinitionForIdentifier<T>(store, definitionIdentifier, out metadata);
	}

	public IEnumerable<TypedContainerItem<TDefinition>> AllDefinitionInfosOfType<TDefinition>() where TDefinition : ObjectDefinition
	{
		HashSet<string> hashSet = new HashSet<string>();
		foreach (AssetPackRuntimeStore store in _stores)
		{
			foreach (ContainerItem @object in store.Container().Objects)
			{
				if (@object.Definition is TDefinition)
				{
					hashSet.Add(@object.Identifier);
				}
			}
		}
		return hashSet.Select(DefinitionForIdentifier<TDefinition>);
	}

	public AbsoluteAssetReference ResolveAssetReference(string contextualDefinitionIdentifier, AssetReference assetReference)
	{
		if (string.IsNullOrEmpty(assetReference.AssetPackIdentifier))
		{
			return new AbsoluteAssetReference(AssetPackIdentifierContainingDefinition(contextualDefinitionIdentifier), assetReference.AssetIdentifier);
		}
		return new AbsoluteAssetReference(assetReference.AssetPackIdentifier, assetReference.AssetIdentifier);
	}

	private void CheckDefinitions()
	{
		HashSet<AssetPackRuntimeStore> hashSet = new HashSet<AssetPackRuntimeStore>();
		foreach (AssetPackRuntimeStore store in _stores)
		{
			try
			{
				foreach (ContainerItem @object in store.Container().Objects)
				{
					DefinitionChecker definitionChecker = new DefinitionChecker(@object.Identifier, store.Identifier, store);
					definitionChecker.Check(@object.Definition);
					definitionChecker.PrintToLog();
				}
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Exception while checking store {store}", store);
				hashSet.Add(store);
			}
		}
		foreach (AssetPackRuntimeStore item in hashSet)
		{
			Log.Warning("Removing store: {store}", item);
			_stores.Remove(item);
		}
	}
}
