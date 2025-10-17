using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AssetPack.Runtime;
using Helpers.Culling;
using KeyValue.Runtime;
using Model;
using Model.Definition;
using Model.Definition.Data;
using Serilog;
using UnityEngine;

namespace Helpers;

[SelectionBase]
[ExecuteInEditMode]
public class SceneryAssetInstance : MonoBehaviour, CullingManager.ICullingEventHandler
{
	[Tooltip("Scenery definition identifier to load.")]
	public string identifier;

	private CullingManager.Token _cullingToken;

	private Task<LoadedAssetReference<GameObject>> _modelLoadTask;

	private bool _wantsLoaded;

	private GameObject _model;

	private string _definitionIdentifier;

	private SceneryDefinition _definition;

	private readonly HashSet<Renderer> _cullRenderers = new HashSet<Renderer>();

	private bool _cullingVisible;

	public event Action<Transform> OnDidLoadModels;

	private void OnEnable()
	{
		SetupIfNeeded();
	}

	private void OnDisable()
	{
		TearDown();
	}

	private void SetupIfNeeded()
	{
		if (!(_definitionIdentifier == identifier))
		{
			TearDown();
			if (SceneryAssetManager.Shared.TryGetSceneryDefinition(identifier, out _definition))
			{
				_definitionIdentifier = identifier;
				_cullingToken = CullingManager.Scenery.AddSphere(base.transform, _definition.CullingRadius, this);
				SetupComponents(ComponentLifetime.Static);
			}
		}
	}

	private void TearDown()
	{
		_definitionIdentifier = null;
		_cullingToken?.Dispose();
		_cullingToken = null;
		SetLoaded(loaded: false);
		base.transform.DestroyAllChildren();
	}

	private void SetupComponents(ComponentLifetime lifetime)
	{
		if (_definition == null)
		{
			return;
		}
		ComponentSetup.Context setupContext = default(ComponentSetup.Context);
		Action<string, Action<Value>> observeProperty = delegate(string key, Action<Value> action)
		{
			AddKeyValueObserver(lifetime, key, action);
		};
		foreach (Model.Definition.Component item in _definition.EnabledComponentsForLifetime(lifetime))
		{
			Transform parent = lifetime switch
			{
				ComponentLifetime.Static => base.transform, 
				ComponentLifetime.Model => _model.transform.ResolveTransform(item.Parent, defaultReturnsReceiver: true), 
				_ => throw new ArgumentOutOfRangeException("lifetime", lifetime, null), 
			};
			ComponentSetup.Setup(identifier, item, setupContext, parent, observeProperty, null);
		}
	}

	private void AddKeyValueObserver(ComponentLifetime lifetime, string key, Action<Value> action)
	{
		Debug.LogWarning("KV not yet supported for scenery: " + key);
	}

	private async void SetLoaded(bool loaded)
	{
		if (loaded == _wantsLoaded)
		{
			return;
		}
		Log.Debug("SetLoaded {identifier}: {loaded}", identifier, loaded);
		_wantsLoaded = loaded;
		if (_wantsLoaded)
		{
			try
			{
				_modelLoadTask = SceneryAssetManager.Shared.LoadScenery(identifier);
				LoadedAssetReference<GameObject> loadedAssetReference = await _modelLoadTask;
				if (!(this == null))
				{
					if (!_wantsLoaded)
					{
						_modelLoadTask.Result.Dispose();
						_modelLoadTask = null;
					}
					else
					{
						Transform transform = base.transform;
						_model = UnityEngine.Object.Instantiate(loadedAssetReference.Asset, transform.position, transform.rotation, transform);
						_model.hideFlags = HideFlags.DontSave;
						DidLoadModel();
					}
				}
				return;
			}
			catch (Exception exception)
			{
				Debug.LogError("Error loading scenery " + identifier);
				Debug.LogException(exception);
				return;
			}
		}
		if (_modelLoadTask != null && _modelLoadTask.IsCompleted)
		{
			_modelLoadTask.Result.Dispose();
			_modelLoadTask = null;
			WillUnloadModel();
			if (Application.isPlaying)
			{
				UnityEngine.Object.Destroy(_model);
			}
			else
			{
				UnityEngine.Object.DestroyImmediate(_model);
			}
			_model = null;
			_cullRenderers.Clear();
		}
	}

	private void DidLoadModel()
	{
		_cullRenderers.UnionWith(_model.GetComponentsInChildren<Renderer>());
		foreach (Renderer cullRenderer in _cullRenderers)
		{
			cullRenderer.enabled = _cullingVisible;
		}
		SetupComponents(ComponentLifetime.Model);
		this.OnDidLoadModels?.Invoke(_model.transform);
	}

	private void WillUnloadModel()
	{
	}

	public static IEnumerable<SceneryAssetInstance> FindInstancesOfIdentifier(string identifier)
	{
		SceneryAssetInstance[] array = UnityEngine.Object.FindObjectsOfType<SceneryAssetInstance>();
		foreach (SceneryAssetInstance sceneryAssetInstance in array)
		{
			if (sceneryAssetInstance.identifier == identifier)
			{
				yield return sceneryAssetInstance;
			}
		}
	}

	public void ReloadComponents()
	{
		SetLoaded(loaded: false);
		base.transform.DestroyAllChildren();
		SetupComponents(ComponentLifetime.Static);
		SetLoaded(loaded: true);
	}

	public void CullingSphereStateChanged(bool isVisible, int distanceBand)
	{
		SetLoaded(distanceBand <= 2);
		bool flag = isVisible && distanceBand < 2;
		if (flag == _cullingVisible)
		{
			return;
		}
		_cullingVisible = flag;
		foreach (Renderer cullRenderer in _cullRenderers)
		{
			cullRenderer.enabled = _cullingVisible;
		}
	}

	public void RequestUpdateCullingPosition()
	{
		_cullingToken.UpdatePosition(base.transform);
	}
}
