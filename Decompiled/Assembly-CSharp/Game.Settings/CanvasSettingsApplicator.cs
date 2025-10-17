using GalaSoft.MvvmLight.Messaging;
using Serilog;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Settings;

[RequireComponent(typeof(CanvasScaler))]
public class CanvasSettingsApplicator : MonoBehaviour
{
	private CanvasScaler _canvasScaler;

	private void Awake()
	{
		_canvasScaler = GetComponent<CanvasScaler>();
	}

	private void OnEnable()
	{
		Messenger.Default.Register<CanvasScaleChanged>(this, delegate
		{
			UpdateCanvasScale();
		});
		UpdateCanvasScale();
	}

	private void OnDisable()
	{
		Messenger.Default.Unregister(this);
	}

	private void UpdateCanvasScale()
	{
		float graphicsCanvasScale = Preferences.GraphicsCanvasScale;
		Log.Debug("GraphicsCanvasScale: {scale}", graphicsCanvasScale);
		_canvasScaler.scaleFactor = graphicsCanvasScale;
	}

	public static void ValidateCanvasScale()
	{
		float graphicsCanvasScale = Preferences.GraphicsCanvasScale;
		float num = MaxCanvasScale();
		if (!(num >= graphicsCanvasScale))
		{
			Log.Warning("Canvas scale {current} is too high for screen height {screenHeight}, setting to {maxCanvasScale}", Preferences.GraphicsCanvasScale, Screen.height, num);
			Preferences.GraphicsCanvasScale = num;
		}
	}

	public static float MaxCanvasScale()
	{
		return Mathf.Clamp(Mathf.Floor((float)Screen.height / 650f / 0.05f) * 0.05f, 0.1f, 2f);
	}
}
