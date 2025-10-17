using GalaSoft.MvvmLight.Messaging;
using Serilog;
using UnityEngine;

namespace Game.Settings;

public class CameraSettingsApplicator : MonoBehaviour
{
	private Camera _camera;

	private void Awake()
	{
		_camera = GetComponent<Camera>();
		UpdateSetting();
	}

	private void OnEnable()
	{
		Messenger.Default.Register<GraphicsDrawDistanceChanged>(this, delegate
		{
			UpdateSetting();
		});
	}

	private void OnDisable()
	{
		Messenger.Default.Unregister(this);
	}

	private void UpdateSetting()
	{
		if (_camera == null)
		{
			Log.Warning("Missing camera!");
		}
		else
		{
			_camera.farClipPlane = Preferences.GraphicsDrawDistance;
		}
	}
}
