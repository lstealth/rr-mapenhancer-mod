using System;
using System.Threading;
using AssetPack.Runtime;
using Model.Database;
using Serilog;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Equipment;

public class BuilderPhoto : MonoBehaviour
{
	[SerializeField]
	private RawImage rawImage;

	private CancellationTokenSource _cancellationTokenSource;

	private LoadedAssetReference<Texture2D> _textureReference;

	private Texture _defaultTexture;

	private void Awake()
	{
		_defaultTexture = rawImage.texture;
	}

	private void OnDestroy()
	{
		_textureReference?.Dispose();
		_textureReference = null;
		_cancellationTokenSource?.Cancel();
		_cancellationTokenSource = null;
	}

	public async void Configure(string carIdentifier)
	{
		_cancellationTokenSource?.Cancel();
		_cancellationTokenSource = null;
		rawImage.texture = _defaultTexture;
		_textureReference?.Dispose();
		_textureReference = null;
		string builderPhotoAssetId = carIdentifier + "--bp0";
		IPrefabStore prefabStore = TrainController.Shared.PrefabStore;
		string assetPackIdentifier = prefabStore.AssetPackIdentifierContainingDefinition(builderPhotoAssetId);
		if (assetPackIdentifier == null)
		{
			Log.Debug("No builder photo found: {carIdentifier} {assetId}", carIdentifier, builderPhotoAssetId);
			return;
		}
		_cancellationTokenSource = new CancellationTokenSource();
		CancellationToken token = _cancellationTokenSource.Token;
		try
		{
			_textureReference = await prefabStore.LoadAssetAsync<Texture2D>(assetPackIdentifier, builderPhotoAssetId, token);
			rawImage.texture = _textureReference.Asset;
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Unable to load builder photo {pack} {assetId}", assetPackIdentifier, builderPhotoAssetId);
		}
		finally
		{
			_cancellationTokenSource = null;
		}
	}
}
