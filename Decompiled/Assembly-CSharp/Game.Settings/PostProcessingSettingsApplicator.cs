using GalaSoft.MvvmLight.Messaging;
using Serilog;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Settings;

[RequireComponent(typeof(Volume))]
public class PostProcessingSettingsApplicator : MonoBehaviour
{
	private ColorAdjustments _colorAdjustments;

	private void Awake()
	{
		if (!GetComponent<Volume>().profile.TryGet<ColorAdjustments>(out _colorAdjustments))
		{
			Log.Error("Couldn't find color adjustments component");
		}
	}

	private void OnEnable()
	{
		Messenger.Default.Register<PostProcessingPreferenceChanged>(this, delegate
		{
			UpdateFromPreferences();
		});
		UpdateFromPreferences();
	}

	private void OnDisable()
	{
		Messenger.Default.Unregister(this);
	}

	private void UpdateFromPreferences()
	{
		if (!(_colorAdjustments == null))
		{
			_colorAdjustments.postExposure.value = Preferences.GraphicsPostExposure;
			_colorAdjustments.contrast.value = Preferences.GraphicsContrast;
		}
	}
}
