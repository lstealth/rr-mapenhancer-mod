using System;
using System.Collections.Generic;
using System.Threading;
using AssetPack.Runtime;
using Helpers.Culling;
using Model;
using Model.Database;
using Model.Definition.Components;
using UnityEngine;

namespace RollingStock;

public class DetailModelController : MonoBehaviour, CullingManager.ICullingEventHandler
{
	private Car _car;

	private readonly List<Renderer> _renderers = new List<Renderer>();

	private CancellationTokenSource _modelLoadCancellationTokenSource;

	private readonly List<LoadedAssetReference<GameObject>> _modelLoadReferences = new List<LoadedAssetReference<GameObject>>();

	private CullingManager.Token _cullingToken;

	private void OnDisable()
	{
		_modelLoadCancellationTokenSource?.Cancel();
		foreach (LoadedAssetReference<GameObject> modelLoadReference in _modelLoadReferences)
		{
			modelLoadReference.Dispose();
		}
		_modelLoadReferences.Clear();
		_cullingToken?.Dispose();
		_cullingToken = null;
	}

	public async void Configure(DetailModelComponent component)
	{
		_modelLoadCancellationTokenSource = new CancellationTokenSource();
		CancellationToken token = _modelLoadCancellationTokenSource.Token;
		IPrefabStore prefabStore = TrainController.Shared.PrefabStore;
		_modelLoadReferences.Clear();
		LoadedAssetReference<GameObject> loadedAssetReference;
		try
		{
			loadedAssetReference = await prefabStore.LoadAssetAsync<GameObject>(component.Model.AssetPackIdentifier, component.Model.AssetIdentifier, token);
		}
		catch (OperationCanceledException)
		{
			return;
		}
		if (!(this == null))
		{
			_modelLoadReferences.Add(loadedAssetReference);
			for (int i = 0; i < component.Count; i++)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(loadedAssetReference.Asset, base.transform, worldPositionStays: false);
				gameObject.transform.localPosition = i * component.Offset;
				_renderers.AddRange(gameObject.GetComponentsInChildren<Renderer>());
			}
			_cullingToken = CullingManager.Scenery.AddSphere(base.transform, component.Offset.magnitude * (float)component.Count + 10f, this);
			_cullingToken.RegisterFixedUpdate(base.transform);
		}
	}

	public void CullingSphereStateChanged(bool isVisible, int distanceBand)
	{
		bool flag = isVisible && distanceBand < 1;
		foreach (Renderer renderer in _renderers)
		{
			renderer.enabled = flag;
		}
	}

	public void RequestUpdateCullingPosition()
	{
		_cullingToken.UpdatePosition(base.transform);
	}
}
