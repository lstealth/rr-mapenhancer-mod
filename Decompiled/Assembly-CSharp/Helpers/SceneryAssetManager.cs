using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AssetPack.Runtime;
using Model.Database;
using Model.Definition;
using Model.Definition.Data;
using UnityEngine;

namespace Helpers;

[ExecuteInEditMode]
public class SceneryAssetManager : MonoBehaviour
{
	private static SceneryAssetManager _instance;

	private PrefabStore _prefabStore;

	public static SceneryAssetManager Shared
	{
		get
		{
			if (_instance == null)
			{
				_instance = Object.FindObjectOfType<SceneryAssetManager>();
			}
			return _instance;
		}
	}

	private IPrefabStore PrefabStore
	{
		get
		{
			if (Application.isPlaying)
			{
				return TrainController.Shared.PrefabStore;
			}
			return _prefabStore ?? (_prefabStore = Model.Database.PrefabStore.Create());
		}
	}

	private void OnDestroy()
	{
		_prefabStore?.Dispose();
		_prefabStore = null;
	}

	public async Task<LoadedAssetReference<GameObject>> LoadScenery(string identifier)
	{
		IPrefabStore prefabStore = PrefabStore;
		string assetPackIdentifier = prefabStore.AssetPackIdentifierContainingDefinition(identifier);
		return await prefabStore.LoadAssetAsync<GameObject>(assetPackIdentifier, identifier, CancellationToken.None);
	}

	public bool TryGetSceneryDefinition(string identifier, out SceneryDefinition sceneryDefinition)
	{
		try
		{
			IPrefabStore prefabStore = PrefabStore;
			sceneryDefinition = prefabStore.DefinitionForIdentifier<SceneryDefinition>(identifier, out var _);
			return true;
		}
		catch
		{
			sceneryDefinition = null;
			return false;
		}
	}

	public List<string> GetSceneryDefinitionIdentifiers()
	{
		return (from item in PrefabStore.AllDefinitionInfosOfType<SceneryDefinition>()
			select item.Identifier into s
			orderby s
			select s).ToList();
	}
}
