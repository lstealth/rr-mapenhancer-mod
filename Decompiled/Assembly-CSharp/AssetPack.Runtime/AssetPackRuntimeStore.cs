using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AssetPack.Common;
using Model.Definition;
using Newtonsoft.Json;
using Serilog;
using UnityEngine;

namespace AssetPack.Runtime;

public class AssetPackRuntimeStore : IDisposable
{
	public enum StoreLocation
	{
		Internal,
		External
	}

	private class LoadRequest
	{
		public AssetBundleRequest Request { get; set; }

		public int ReferenceCount { get; set; }
	}

	private AssetPackCatalog? _catalog;

	private Container _container;

	private Task<AssetBundle> _loadAssetBundleTask;

	private AssetBundle _staticAssetBundle;

	private bool _disposed;

	private readonly Dictionary<string, LoadRequest> _loadRequests = new Dictionary<string, LoadRequest>();

	public string Identifier { get; private set; }

	public StoreLocation Location { get; private set; }

	private string BasePath => Path.Combine(BasePathForLocation(Location), Identifier);

	private string AssetBundlePath => Path.Combine(BasePath, "Bundle");

	private string CatalogPath => Path.Combine(BasePath, "Catalog.json");

	private string DefinitionsPath => Path.Combine(BasePath, "Definitions.json");

	public static string BasePathForLocation(StoreLocation location)
	{
		return location switch
		{
			StoreLocation.Internal => Path.Combine(Application.streamingAssetsPath, "AssetPacks"), 
			StoreLocation.External => Path.Combine(Application.persistentDataPath, "AssetPacks"), 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	public AssetPackRuntimeStore(string identifier, StoreLocation location)
	{
		Identifier = identifier;
		Location = location;
	}

	public void Dispose()
	{
		if (_loadAssetBundleTask != null && _loadAssetBundleTask.Result != null)
		{
			_loadAssetBundleTask.Result.Unload(unloadAllLoadedObjects: true);
		}
		_loadAssetBundleTask?.Dispose();
		_loadAssetBundleTask = null;
		if (_staticAssetBundle != null)
		{
			_staticAssetBundle.Unload(unloadAllLoadedObjects: true);
			_staticAssetBundle = null;
		}
		_disposed = true;
	}

	public override string ToString()
	{
		return $"{Location} {Identifier}";
	}

	public AssetPackCatalog Catalog()
	{
		if (_catalog.HasValue)
		{
			return _catalog.Value;
		}
		AssetPackCatalog assetPackCatalog = JsonConvert.DeserializeObject<AssetPackCatalog>(File.ReadAllText(CatalogPath));
		_catalog = assetPackCatalog;
		return assetPackCatalog;
	}

	public void LoadBundleStatic()
	{
		Log.Debug("LoadBundleStatic {identifier} from {path}", Identifier, AssetBundlePath);
		_staticAssetBundle = AssetBundle.LoadFromFile(AssetBundlePath);
		if (_staticAssetBundle == null)
		{
			Log.Error("Unable to load bundle {identifier} from {path}", Identifier, AssetBundlePath);
		}
	}

	private Task<AssetBundle> LoadedBundle()
	{
		if (_loadAssetBundleTask != null)
		{
			return _loadAssetBundleTask;
		}
		TaskCompletionSource<AssetBundle> tcs = new TaskCompletionSource<AssetBundle>();
		if (_staticAssetBundle != null)
		{
			tcs.SetResult(_staticAssetBundle);
		}
		else
		{
			AssetBundle.LoadFromFileAsync(AssetBundlePath).completed += delegate(AsyncOperation obj)
			{
				if (obj is AssetBundleCreateRequest assetBundleCreateRequest && assetBundleCreateRequest.assetBundle != null)
				{
					tcs.SetResult(assetBundleCreateRequest.assetBundle);
				}
				else
				{
					tcs.SetException(new Exception("Failed to load asset bundle: " + AssetBundlePath));
				}
			};
		}
		_loadAssetBundleTask = tcs.Task;
		return _loadAssetBundleTask;
	}

	public async Task<LoadedAssetReference<T>> LoadAsset<T>(string assetIdentifier, CancellationToken cancellationToken) where T : UnityEngine.Object
	{
		AssetBundle assetBundle;
		try
		{
			assetBundle = await LoadedBundle();
			cancellationToken.ThrowIfCancellationRequested();
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Exception loading AssetBundle for {assetIdentifier}:", assetIdentifier);
			throw;
		}
		if (_loadRequests.TryGetValue(assetIdentifier, out var value))
		{
			value.ReferenceCount++;
			Log.Debug("ABRS LoadAsset {assetIdentifier}: {loadRequestReferenceCount} (existing)", assetIdentifier, value.ReferenceCount);
		}
		else
		{
			Log.Information("ABRS LoadAsset {assetIdentifier}: New", assetIdentifier);
			AssetBundleRequest request;
			try
			{
				request = assetBundle.LoadAssetAsync<T>(assetIdentifier);
				cancellationToken.ThrowIfCancellationRequested();
			}
			catch (Exception exception2)
			{
				Log.Error(exception2, "Exception starting load of asset {assetIdentifier}:", assetIdentifier);
				throw;
			}
			value = new LoadRequest
			{
				Request = request,
				ReferenceCount = 1
			};
			_loadRequests[assetIdentifier] = value;
		}
		try
		{
			return new LoadedAssetReference<T>((await value.Request) as T, this, assetIdentifier);
		}
		catch (Exception exception3)
		{
			Log.Error(exception3, "Exception awaiting load of asset {assetIdentifier}:", assetIdentifier);
			DecrementReferenceCount(assetIdentifier);
			throw;
		}
	}

	public Container Container()
	{
		if (_container != null)
		{
			return _container;
		}
		if (!File.Exists(DefinitionsPath))
		{
			_container = new Container();
			return _container;
		}
		string text = File.ReadAllText(DefinitionsPath);
		try
		{
			return _container = ContainerSerialization.Deserialize(text);
		}
		catch (Exception)
		{
			Debug.LogError("Error deserializing definitions for " + Identifier + " from " + DefinitionsPath + ":");
			throw;
		}
	}

	public void AddItem(ContainerItem item)
	{
		Container().Objects.Add(item);
	}

	public ContainerItem ContainerItemForObjectIdentifier(string objectIdentifier)
	{
		foreach (ContainerItem @object in Container().Objects)
		{
			if (@object.Identifier == objectIdentifier)
			{
				return @object;
			}
		}
		return null;
	}

	public void SaveContainer()
	{
		if (Location != StoreLocation.External)
		{
			throw new Exception("Store be external");
		}
		string text = ContainerSerialization.Serialize(_container);
		File.WriteAllText(DefinitionsPath, text);
		Debug.Log($"Wrote {text.Length} bytes to {DefinitionsPath}");
	}

	public void DecrementReferenceCount(string identifier)
	{
		if (_disposed)
		{
			return;
		}
		if (_loadRequests.TryGetValue(identifier, out var value))
		{
			value.ReferenceCount--;
			Log.Debug("ABRS Decrement {identifier}: {referenceCount}", identifier, value.ReferenceCount);
			if (_loadRequests.Values.Select((LoadRequest v) => v.ReferenceCount).Sum() <= 0)
			{
				UnloadAssetBundleWithNoRemainingReferences();
				_loadRequests.Clear();
			}
		}
		else
		{
			Log.Error("Request for identifier not found: {identifier}", identifier);
		}
	}

	private void UnloadAssetBundleWithNoRemainingReferences()
	{
		if (_staticAssetBundle != null)
		{
			return;
		}
		if (_loadAssetBundleTask == null)
		{
			Log.Error("ABRS Can't unload bundle - no bundle task");
			return;
		}
		if (_loadAssetBundleTask.Result != null)
		{
			Log.Debug("ABRS Unload bundle {bundle}", _loadAssetBundleTask.Result.name);
			_loadAssetBundleTask.Result.Unload(unloadAllLoadedObjects: true);
		}
		else
		{
			Log.Warning("ABRS Can't unload null asset bundle result {identifier}", Identifier);
		}
		_loadAssetBundleTask.Dispose();
		_loadAssetBundleTask = null;
	}

	public bool ContainsIdentifier(string identifier)
	{
		return ContainerItemForObjectIdentifier(identifier) != null;
	}
}
