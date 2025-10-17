using System;
using Analytics;
using Avatar;
using GalaSoft.MvvmLight.Messaging;
using Game.Settings;
using Helpers;
using Serilog;
using UnityEngine;

namespace Game;

public static class Preferences
{
	public enum AnalyticsPref
	{
		Unknown,
		OptIn,
		OptOut
	}

	public enum ParticleLevel
	{
		Off,
		Low,
		Standard
	}

	public enum GraphicsVsyncOption
	{
		SixtyFps = -1,
		DontSync,
		VsyncEveryFrame,
		VsyncEveryOther
	}

	private const string KeyAnalytics = "analytics";

	private const string KeyAvatarDescriptor = "avatar.descriptor";

	private const string KeyGfxTreeDensity = "gfx.tree.density";

	private const string KeyGfxDetailDensity = "gfx.detail.density";

	private const string KeyGfxAntiAliasing = "gfx.msaa";

	private const string KeyGfxVsync = "gfx.vsync";

	private const string KeyGfxFPSLimit = "gfx.fps.limit";

	private const string KeyGfxCanvasScale = "gfx.canvas.scale";

	private const string KeyGfxNightLightLevel = "gfx.night-light-level";

	private const string KeyGfxPostExposure = "gfx.post-exp";

	private const string KeyGfxContrast = "gfx.contrast";

	private const string KeySimplifiedControls = "controls.simplified";

	private const string KeyCarUpdateOptimization = "car.update-opt";

	private const string KeyHostAuthLogging = "host-auth-logging";

	private const string KeySwayIntensity = "camera.sway.intensity";

	private static float? _cameraSwayIntensity;

	private const string KeyMouseLookSpeed = "camera.look.speed";

	private const string KeyMouseLookInvert = "camera.look.invert";

	private const string KeyMouseLookToggle = "camera.look.toggle";

	private static float? _mouseLookSpeed;

	private static bool? _mouseLookInvert;

	private static bool? _mouseLookToggle;

	private const string KeySoundVolumeMain = "sound.volume.main";

	private const string KeySoundVolumeEngine = "sound.volume.engine";

	private const string KeySoundVolumeWhistle = "sound.volume.whistle";

	private const string KeySoundVolumeBell = "sound.volume.bell";

	private const string KeySoundVolumeDynamo = "sound.volume.dynamo";

	private const string KeySoundVolumeEnvironment = "sound.volume.environment";

	private const string KeySoundVolumeCtcBell = "sound.volume.ctc-bell";

	private const string KeySoundVolumeWheels = "sound.volume.wheels";

	private const float VolumeDefault = 1f;

	public static AnalyticsPref Analytics
	{
		get
		{
			return (AnalyticsPref)PlayerPrefs.GetInt("analytics", 0);
		}
		set
		{
			PlayerPrefs.SetInt("analytics", (int)value);
			Messenger.Default.Send(default(AnalyticsPreferenceDidChange));
		}
	}

	public static AvatarDescriptor AvatarDescriptor
	{
		get
		{
			string text = PlayerPrefs.GetString("avatar.descriptor");
			if (string.IsNullOrEmpty(text))
			{
				return AvatarDescriptor.Default;
			}
			try
			{
				return AvatarDescriptor.From(KeyValueJson.ValueForString(text));
			}
			catch (Exception exception)
			{
				Log.Warning(exception, "Error deserializing AvatarDescriptor");
				Debug.LogException(exception);
				return AvatarDescriptor.Default;
			}
		}
		set
		{
			string value2 = KeyValueJson.StringFromValue(value.ToValue());
			PlayerPrefs.SetString("avatar.descriptor", value2);
		}
	}

	public static string MultiplayerLobbyName
	{
		get
		{
			return PlayerPrefs.GetString("multiplayer.lobby.name");
		}
		set
		{
			PlayerPrefs.SetString("multiplayer.lobby.name", value);
		}
	}

	public static int MultiplayerLobbyType
	{
		get
		{
			return PlayerPrefs.GetInt("multiplayer.lobby.type");
		}
		set
		{
			PlayerPrefs.SetInt("multiplayer.lobby.type", value);
		}
	}

	public static string MultiplayerClientUsername
	{
		get
		{
			return PlayerPrefs.GetString("multiplayer.client.username", "");
		}
		set
		{
			PlayerPrefs.SetString("multiplayer.client.username", value);
		}
	}

	public static float CameraSwayIntensity
	{
		get
		{
			if (!_cameraSwayIntensity.HasValue)
			{
				_cameraSwayIntensity = PlayerPrefs.GetFloat("camera.sway.intensity", 1f);
			}
			return _cameraSwayIntensity.Value;
		}
		set
		{
			_cameraSwayIntensity = value;
			PlayerPrefs.SetFloat("camera.sway.intensity", value);
		}
	}

	public static float MouseLookSpeed
	{
		get
		{
			float valueOrDefault = _mouseLookSpeed.GetValueOrDefault();
			if (!_mouseLookSpeed.HasValue)
			{
				valueOrDefault = PlayerPrefs.GetFloat("camera.look.speed", 1f);
				_mouseLookSpeed = valueOrDefault;
			}
			return _mouseLookSpeed.Value;
		}
		set
		{
			_mouseLookSpeed = value;
			PlayerPrefs.SetFloat("camera.look.speed", value);
		}
	}

	public static bool MouseLookInvert
	{
		get
		{
			bool valueOrDefault = _mouseLookInvert == true;
			if (!_mouseLookInvert.HasValue)
			{
				valueOrDefault = GetBool("camera.look.invert", defaultValue: false);
				_mouseLookInvert = valueOrDefault;
			}
			return _mouseLookInvert.Value;
		}
		set
		{
			_mouseLookInvert = value;
			SetBool("camera.look.invert", value);
		}
	}

	public static bool MouseLookToggle
	{
		get
		{
			bool valueOrDefault = _mouseLookToggle == true;
			if (!_mouseLookToggle.HasValue)
			{
				valueOrDefault = GetBool("camera.look.toggle", defaultValue: false);
				_mouseLookToggle = valueOrDefault;
			}
			return _mouseLookToggle.Value;
		}
		set
		{
			_mouseLookToggle = value;
			SetBool("camera.look.toggle", value);
		}
	}

	public static bool HostAuthLogging
	{
		get
		{
			return GetBool("host-auth-logging", defaultValue: false);
		}
		set
		{
			SetBool("host-auth-logging", value);
		}
	}

	public static float DefaultFOV
	{
		get
		{
			return PlayerPrefs.GetFloat("ui.fov0", 40f);
		}
		set
		{
			PlayerPrefs.SetFloat("ui.fov0", value);
		}
	}

	public static float AlternateFOV
	{
		get
		{
			return PlayerPrefs.GetFloat("ui.fov1", 80f);
		}
		set
		{
			PlayerPrefs.SetFloat("ui.fov1", value);
		}
	}

	public static bool ShowCompass
	{
		get
		{
			return GetBool("ui.compass", defaultValue: true);
		}
		set
		{
			SetBool("ui.compass", value);
		}
	}

	public static bool ShowClockAlways
	{
		get
		{
			return GetBool("ui.clock.always", defaultValue: true);
		}
		set
		{
			SetBool("ui.clock.always", value);
		}
	}

	public static float GraphicsDrawDistance
	{
		get
		{
			return PlayerPrefs.GetFloat("gfx.drawdistance", 1500f);
		}
		set
		{
			PlayerPrefs.SetFloat("gfx.drawdistance", Mathf.Clamp(value, 100f, 10000f));
			Messenger.Default.Send(default(GraphicsDrawDistanceChanged));
		}
	}

	public static ParticleLevel GraphicsParticleLevel
	{
		get
		{
			return (ParticleLevel)PlayerPrefs.GetInt("gfx.particlelevel", 2);
		}
		set
		{
			PlayerPrefs.SetInt("gfx.particlelevel", (int)value);
		}
	}

	public static float GraphicsTreeDensity
	{
		get
		{
			return PlayerPrefs.GetFloat("gfx.tree.density", 1f);
		}
		set
		{
			PlayerPrefs.SetFloat("gfx.tree.density", value);
		}
	}

	public static float GraphicsDetailDensity
	{
		get
		{
			return PlayerPrefs.GetFloat("gfx.detail.density", 1f);
		}
		set
		{
			PlayerPrefs.SetFloat("gfx.detail.density", value);
		}
	}

	public static GraphicsVsyncOption GraphicsVsync
	{
		get
		{
			return (GraphicsVsyncOption)PlayerPrefs.GetInt("gfx.vsync", -1);
		}
		set
		{
			PlayerPrefs.SetInt("gfx.vsync", (int)value);
			FireGraphicsSettingsChanged();
		}
	}

	public static float GraphicsCanvasScale
	{
		get
		{
			return PlayerPrefs.GetFloat("gfx.canvas.scale", 1f);
		}
		set
		{
			PlayerPrefs.SetFloat("gfx.canvas.scale", value);
			Messenger.Default.Send(default(CanvasScaleChanged));
		}
	}

	public static float GraphicsNightLightLevel
	{
		get
		{
			return PlayerPrefs.GetFloat("gfx.night-light-level", 0.3f);
		}
		set
		{
			PlayerPrefs.SetFloat("gfx.night-light-level", value);
			Messenger.Default.Send(default(EnviroSettingChanged));
		}
	}

	public static float GraphicsPostExposure
	{
		get
		{
			return PlayerPrefs.GetFloat("gfx.post-exp", 0.5f);
		}
		set
		{
			PlayerPrefs.SetFloat("gfx.post-exp", value);
			Messenger.Default.Send(default(PostProcessingPreferenceChanged));
		}
	}

	public static float GraphicsContrast
	{
		get
		{
			return PlayerPrefs.GetFloat("gfx.contrast", 25f);
		}
		set
		{
			PlayerPrefs.SetFloat("gfx.contrast", value);
			Messenger.Default.Send(default(PostProcessingPreferenceChanged));
		}
	}

	public static float SoundVolumeMain
	{
		get
		{
			return PlayerPrefs.GetFloat("sound.volume.main", 0.8f);
		}
		set
		{
			PlayerPrefs.SetFloat("sound.volume.main", value);
			Messenger.Default.Send(default(SoundVolumeChanged));
		}
	}

	public static float SoundVolumeEngine
	{
		get
		{
			return PlayerPrefs.GetFloat("sound.volume.engine", 1f);
		}
		set
		{
			PlayerPrefs.SetFloat("sound.volume.engine", value);
			Messenger.Default.Send(default(SoundVolumeChanged));
		}
	}

	public static float SoundVolumeWhistle
	{
		get
		{
			return PlayerPrefs.GetFloat("sound.volume.whistle", 1f);
		}
		set
		{
			PlayerPrefs.SetFloat("sound.volume.whistle", value);
			Messenger.Default.Send(default(SoundVolumeChanged));
		}
	}

	public static float SoundVolumeBell
	{
		get
		{
			return PlayerPrefs.GetFloat("sound.volume.bell", 1f);
		}
		set
		{
			PlayerPrefs.SetFloat("sound.volume.bell", value);
			Messenger.Default.Send(default(SoundVolumeChanged));
		}
	}

	public static float SoundVolumeDynamo
	{
		get
		{
			return PlayerPrefs.GetFloat("sound.volume.dynamo", 1f);
		}
		set
		{
			PlayerPrefs.SetFloat("sound.volume.dynamo", value);
			Messenger.Default.Send(default(SoundVolumeChanged));
		}
	}

	public static float SoundVolumeEnvironment
	{
		get
		{
			return PlayerPrefs.GetFloat("sound.volume.environment", 1f);
		}
		set
		{
			PlayerPrefs.SetFloat("sound.volume.environment", value);
			Messenger.Default.Send(default(SoundVolumeChanged));
		}
	}

	public static float SoundVolumeCtcBell
	{
		get
		{
			return PlayerPrefs.GetFloat("sound.volume.ctc-bell", 1f);
		}
		set
		{
			PlayerPrefs.SetFloat("sound.volume.ctc-bell", value);
			Messenger.Default.Send(default(SoundVolumeChanged));
		}
	}

	public static float SoundVolumeWheels
	{
		get
		{
			return PlayerPrefs.GetFloat("sound.volume.wheels", 1f);
		}
		set
		{
			PlayerPrefs.SetFloat("sound.volume.wheels", value);
			Messenger.Default.Send(default(SoundVolumeChanged));
		}
	}

	public static bool SimplifiedControls
	{
		get
		{
			return GetBool("controls.simplified", defaultValue: false);
		}
		set
		{
			SetBool("controls.simplified", value);
		}
	}

	public static bool EnableCarUpdateOptimization
	{
		get
		{
			return GetBool("car.update-opt", defaultValue: false);
		}
		set
		{
			SetBool("car.update-opt", value);
		}
	}

	private static void FireGraphicsSettingsChanged()
	{
		Messenger.Default.Send(default(GraphicsSettingsChanged));
	}

	private static bool GetBool(string key, bool defaultValue)
	{
		return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) != 0;
	}

	private static void SetBool(string key, bool value)
	{
		PlayerPrefs.SetInt(key, value ? 1 : 0);
	}
}
