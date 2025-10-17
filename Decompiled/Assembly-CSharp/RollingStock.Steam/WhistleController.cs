using System;
using System.Threading;
using AssetPack.Runtime;
using Audio;
using KeyValue.Runtime;
using Model.Database;
using Model.Definition;
using Model.Definition.Components;
using Model.Definition.Data;
using Serilog;
using UnityEngine;
using UnityEngine.VFX;

namespace RollingStock.Steam;

public class WhistleController : MonoBehaviour
{
	public VisualEffect visualEffect;

	public WhistlePlayer whistlePlayer;

	private GameObject _whistleModel;

	private LoadedAssetReference<GameObject> _modelLoadReference;

	private LoadedAssetReference<AudioClip> _audioLoadReference;

	private CancellationTokenSource _loadCancellationTokenSource;

	private KeyValueObject _keyValueObject;

	private IDisposable _customizationObserver;

	public SmokeEffectWrapper EffectWrapper => new SmokeEffectWrapper(visualEffect);

	private void Awake()
	{
		_keyValueObject = GetComponentInParent<KeyValueObject>();
	}

	private void OnEnable()
	{
		SmokeEffectWrapper effectWrapper = EffectWrapper;
		if (effectWrapper.IsValid)
		{
			effectWrapper.Rate = 0f;
			effectWrapper.Velocity = 10f;
			effectWrapper.Lifetime = 0.5f;
			effectWrapper.TurbulenceIntensity = 50f;
			effectWrapper.Size0 = 0.3f;
			effectWrapper.Size1 = 1f;
		}
	}

	private void OnDisable()
	{
		_customizationObserver?.Dispose();
		_customizationObserver = null;
	}

	private void OnDestroy()
	{
		DisposeModelLoadReference();
	}

	public void Configure(WhistleComponent whistleComponent)
	{
		if ((object)_keyValueObject == null)
		{
			_keyValueObject = GetComponentInParent<KeyValueObject>();
		}
		_customizationObserver = _keyValueObject.Observe("whistle.custom", delegate(Value value)
		{
			WhistleCustomizationSettings settings = WhistleCustomizationSettings.FromPropertyValue(value) ?? new WhistleCustomizationSettings(whistleComponent.DefaultWhistleIdentifier);
			Configure(settings);
		});
	}

	private async void Configure(WhistleCustomizationSettings settings)
	{
		if (_loadCancellationTokenSource != null)
		{
			_loadCancellationTokenSource?.Cancel();
			_loadCancellationTokenSource = null;
		}
		if (_whistleModel != null)
		{
			UnityEngine.Object.Destroy(_whistleModel);
			_whistleModel = null;
		}
		DisposeModelLoadReference();
		string whistleIdentifier = settings.WhistleIdentifier;
		IPrefabStore prefabStore = TrainController.Shared.PrefabStore;
		ObjectMetadata metadata;
		WhistleDefinition whistleDefinition = prefabStore.DefinitionForIdentifier<WhistleDefinition>(whistleIdentifier, out metadata);
		if (!whistleDefinition.Model.IsEmpty)
		{
			_loadCancellationTokenSource = new CancellationTokenSource();
			CancellationToken token = _loadCancellationTokenSource.Token;
			AbsoluteAssetReference assetReference = prefabStore.ResolveAssetReference(whistleIdentifier, whistleDefinition.Model);
			try
			{
				_modelLoadReference = await prefabStore.LoadAssetAsync<GameObject>(assetReference, token);
			}
			catch (OperationCanceledException)
			{
				return;
			}
			if (this == null)
			{
				return;
			}
			_whistleModel = UnityEngine.Object.Instantiate(_modelLoadReference.Asset, base.transform, worldPositionStays: false);
		}
		if (!whistleDefinition.Audio.IsEmpty)
		{
			_loadCancellationTokenSource = new CancellationTokenSource();
			CancellationToken token2 = _loadCancellationTokenSource.Token;
			AbsoluteAssetReference assetReference2 = prefabStore.ResolveAssetReference(whistleIdentifier, whistleDefinition.Audio);
			_audioLoadReference = await prefabStore.LoadAssetAsync<AudioClip>(assetReference2, token2);
			if (this == null)
			{
				Log.Warning("WhistleController destroyed while loading model.");
			}
			else
			{
				whistlePlayer.Configure(_audioLoadReference.Asset);
			}
		}
	}

	private void DisposeModelLoadReference()
	{
		if (_modelLoadReference != null)
		{
			_modelLoadReference.Dispose();
			_modelLoadReference = null;
		}
	}
}
