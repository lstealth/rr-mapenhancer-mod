using System;
using System.Collections;
using Core;
using GalaSoft.MvvmLight.Messaging;
using Game;
using Game.Events;
using Helpers;
using Map.Runtime;
using Serilog;
using UnityEngine;

namespace Cameras;

[ExecuteInEditMode]
public class MapCameraUpdater : MonoBehaviour
{
	public MapManager mapManager;

	private Camera _camera;

	private CoroutineTask _updateCoroutine;

	private void OnEnable()
	{
		_updateCoroutine = CoroutineTask.Start(UpdateCoroutine(), this);
		Messenger.Default.Register<WorldDidMoveEvent>(this, WorldMoved);
	}

	private void OnDisable()
	{
		_updateCoroutine.Stop();
		Messenger.Default.Unregister(this);
	}

	private IEnumerator UpdateCoroutine()
	{
		WaitForSecondsRealtime wait = new WaitForSecondsRealtime(1f);
		while (mapManager == null)
		{
			yield return new WaitForSecondsRealtime(0.2f);
		}
		mapManager.SetDensity(Preferences.GraphicsTreeDensity, Preferences.GraphicsDetailDensity);
		while (true)
		{
			try
			{
				Vector3 position = GetCamera().transform.position;
				mapManager.UpdateVisibleTilesForPosition(WorldTransformer.WorldToGame(position));
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Exception while updating visible tiles:");
			}
			yield return wait;
		}
	}

	private Camera GetCamera()
	{
		if (_camera != null)
		{
			return _camera;
		}
		if (_camera == null)
		{
			_camera = Camera.main;
		}
		return _camera;
	}

	private void WorldMoved(WorldDidMoveEvent evt)
	{
		mapManager.ApplyWorldToGameOffset(evt.Offset);
	}

	public static void SetTerrainDensityValues(float treeDensity, float detailDensity)
	{
		Preferences.GraphicsTreeDensity = treeDensity;
		Preferences.GraphicsDetailDensity = detailDensity;
		MapManager instance = MapManager.Instance;
		if (!(instance == null))
		{
			instance.SetDensity(treeDensity, detailDensity);
			instance.RebuildAll();
		}
	}
}
