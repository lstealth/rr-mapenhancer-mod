using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using AssetPack.Common;
using UnityEngine;

namespace AssetPack.Runtime;

public class AssetPackRuntimeTester : MonoBehaviour
{
	private AssetPackRuntimeStore _store;

	private void Start()
	{
		_store = new AssetPackRuntimeStore("test1", AssetPackRuntimeStore.StoreLocation.External);
		LoadAndInstantiateAll();
	}

	private void OnDestroy()
	{
		_store?.Dispose();
	}

	private void OnGUI()
	{
		if (GUILayout.Button("Instance"))
		{
			LoadAndInstantiateAll();
		}
	}

	private async void LoadAndInstantiateAll()
	{
		try
		{
			foreach (KeyValuePair<string, Asset> asset2 in _store.Catalog().assets)
			{
				asset2.Deconstruct(out var key, out var value);
				string assetIdentifier = key;
				Asset asset = value;
				GameObject obj = UnityEngine.Object.Instantiate((await _store.LoadAsset<GameObject>(assetIdentifier, CancellationToken.None)).Asset, base.transform);
				obj.name = asset.name;
				obj.transform.position = UnityEngine.Random.insideUnitSphere * 10f;
			}
		}
		catch (Exception value2)
		{
			System.Console.WriteLine(value2);
			throw;
		}
	}

	private IEnumerator Direct()
	{
		string path = Path.Combine(Application.streamingAssetsPath, "test1.box");
		AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(path);
		yield return request;
		AssetBundle assetBundle = request.assetBundle;
		if (assetBundle == null)
		{
			Debug.LogError("Failed to load asset bundle");
			yield break;
		}
		UnityEngine.Object.Instantiate(assetBundle.LoadAsset<GameObject>("cube2"), base.transform).transform.position = Vector3.zero;
		assetBundle.Unload(unloadAllLoadedObjects: false);
	}
}
