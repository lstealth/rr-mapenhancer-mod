using System.Collections;
using GalaSoft.MvvmLight.Messaging;
using Serilog;
using UnityEngine;

namespace Game.Settings;

public class GraphicsSettingsApplicator : MonoBehaviour
{
	private Coroutine _refreshCoroutine;

	private void OnEnable()
	{
		Messenger.Default.Register<GraphicsSettingsChanged>(this, delegate
		{
			UpdateSettings();
		});
		Messenger.Default.Register<CanvasScaleChanged>(this, delegate
		{
			CanvasSettingsApplicator.ValidateCanvasScale();
		});
		UpdateSettings();
		_refreshCoroutine = StartCoroutine(RefreshCoroutine());
	}

	private void OnDisable()
	{
		Messenger.Default.Unregister(this);
		StopCoroutine(_refreshCoroutine);
	}

	private void UpdateSettings()
	{
		Preferences.GraphicsVsyncOption graphicsVsync = Preferences.GraphicsVsync;
		QualitySettings.vSyncCount = graphicsVsync switch
		{
			Preferences.GraphicsVsyncOption.SixtyFps => 0, 
			Preferences.GraphicsVsyncOption.DontSync => 0, 
			Preferences.GraphicsVsyncOption.VsyncEveryFrame => 1, 
			Preferences.GraphicsVsyncOption.VsyncEveryOther => 2, 
			_ => 0, 
		};
		int targetFrameRate = ((graphicsVsync != Preferences.GraphicsVsyncOption.SixtyFps) ? 240 : 60);
		Application.targetFrameRate = targetFrameRate;
		Log.Information("vSyncCount = {vSyncCount}, targetFrameRate = {targetFrameRate}, antiAliasing = {msaa}", QualitySettings.vSyncCount, Application.targetFrameRate, QualitySettings.antiAliasing);
	}

	private IEnumerator RefreshCoroutine()
	{
		WaitForSecondsRealtime wait = new WaitForSecondsRealtime(1f);
		Vector2Int lastScreenSize = Vector2Int.zero;
		while (base.isActiveAndEnabled)
		{
			Vector2Int vector2Int = new Vector2Int(Screen.width, Screen.height);
			if (vector2Int != lastScreenSize)
			{
				CanvasSettingsApplicator.ValidateCanvasScale();
				lastScreenSize = vector2Int;
			}
			yield return wait;
		}
	}
}
