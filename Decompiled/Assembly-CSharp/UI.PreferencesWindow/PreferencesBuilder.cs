using System;
using System.Collections.Generic;
using System.Linq;
using Cameras;
using GalaSoft.MvvmLight.Messaging;
using Game;
using Game.Events;
using Game.Settings;
using UI.Builder;
using UI.CompanyWindow;
using UnityEngine;

namespace UI.PreferencesWindow;

public static class PreferencesBuilder
{
	private static float _pendingTreeDensity;

	private static float _pendingDetailDensity;

	private static int _applyDensityAfterDelay;

	private static readonly UIState<string> SelectedTab = new UIState<string>("char");

	private static IConfigurableElement _uiScaleRectTransform;

	private static float _uiScaleValue;

	public static void Build(UIPanelBuilder builder)
	{
		builder.AddTabbedPanels(SelectedTab, BuildTabs);
	}

	private static void BuildTabs(UITabbedPanelBuilder builder)
	{
		builder.AddTab("Character", "char", CharacterSettingsBuilder.BuildCharacterPanel);
		builder.AddTab("Graphics", "gfx", delegate(UIPanelBuilder b)
		{
			b.VScrollView(BuildTabGraphics, new RectOffset(0, 4, 0, 0));
		});
		builder.AddTab("Sound", "sound", BuildTabSound);
		builder.AddTab("Input", "input", BuildTabInput);
		builder.AddTab("Features", "features", BuildTabFeatures);
	}

	private static void BuildOtherSection(UIPanelBuilder builder)
	{
		builder.AddSection("Experimental");
		builder.AddField("Car Update Optimization", builder.AddToggle(() => Preferences.EnableCarUpdateOptimization, delegate(bool b)
		{
			Preferences.EnableCarUpdateOptimization = b;
		})).Tooltip("Car Update Optimization", "Optimize how cars are moved. Can save CPU time on particularly busy railroads, but may lead to jittery car movements.");
		builder.AddSection("Other");
		builder.AddField("Collect Usage Data", builder.AddToggle(() => Preferences.Analytics == Preferences.AnalyticsPref.OptIn, delegate(bool b)
		{
			Preferences.Analytics = (b ? Preferences.AnalyticsPref.OptIn : Preferences.AnalyticsPref.OptOut);
		})).Tooltip("Collect Usage Data", "Anonymously collect usage data to help us improve Railroader. No customization information is collected.");
	}

	private static void BuildBehaviorSection(UIPanelBuilder builder)
	{
		builder.AddSection("Behavior & Features");
		builder.AddField("Sway Intensity", builder.AddSlider(() => Preferences.CameraSwayIntensity, () => Preferences.CameraSwayIntensity.ToString("F1"), delegate(float value)
		{
			Preferences.CameraSwayIntensity = value;
		}));
		builder.AddField("Compass", builder.AddToggle(() => Preferences.ShowCompass, delegate(bool value)
		{
			Preferences.ShowCompass = value;
			Messenger.Default.Send(default(UISettingDidChange));
		}));
		builder.AddField("Always Show Clock", builder.AddToggle(() => Preferences.ShowClockAlways, delegate(bool value)
		{
			Preferences.ShowClockAlways = value;
			Messenger.Default.Send(default(UISettingDidChange));
		}));
	}

	private static void BuildTabInput(UIPanelBuilder builder)
	{
		builder.AddField("Mouse Look", builder.AddDropdown(new List<string> { "Hold Right Mouse", "Right Mouse Toggles" }, Preferences.MouseLookToggle ? 1 : 0, delegate(int i)
		{
			Preferences.MouseLookToggle = i == 1;
		}));
		builder.AddField("Mouse Look Speed", builder.AddSlider(() => Preferences.MouseLookSpeed, () => Preferences.MouseLookSpeed.ToString("F1"), delegate(float value)
		{
			Preferences.MouseLookSpeed = value;
		}, 0.5f, 5f));
		builder.AddField("Invert Mouse", builder.AddToggle(() => Preferences.MouseLookInvert, delegate(bool b)
		{
			Preferences.MouseLookInvert = b;
		}));
		if (BindingsWindow.CanShow)
		{
			IConfigurableElement configurableElement = builder.AddButton("Customize Bindings", BindingsWindow.Show);
			builder.AddField("Controls", configurableElement.RectTransform);
		}
		else
		{
			builder.AddField("Controls", "Bindings Customization only available in game.");
		}
		builder.AddExpandingVerticalSpacer();
	}

	private static void BuildTabGraphics(UIPanelBuilder builder)
	{
		builder.Spacing = 2f;
		List<Resolution> resolutions = (from r in Screen.resolutions
			group r by (width: r.width, height: r.height) into t
			select t.First()).ToList();
		List<int> values = resolutions.Select((Resolution r, int i) => i).ToList();
		int selected = resolutions.FindIndex((Resolution res) => res.width == Screen.width && res.height == Screen.height);
		builder.AddField("Resolution", builder.AddDropdownIntPicker(values, selected, (int i) => (i >= 0) ? $"{resolutions[i].width} x {resolutions[i].height}" : $"{Screen.width} x {Screen.height}", canWrite: true, delegate(int i)
		{
			Resolution resolution = resolutions[i];
			Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
		}));
		builder.AddField("Full Screen", builder.AddToggle(() => Screen.fullScreen, delegate(bool en)
		{
			Screen.fullScreen = en;
		}));
		_uiScaleValue = Preferences.GraphicsCanvasScale;
		_uiScaleRectTransform = builder.AddField("UI Scale", builder.AddSliderQuantized(() => _uiScaleValue, () => $"{Mathf.Round(_uiScaleValue * 100f)}%", delegate(float f)
		{
			_uiScaleValue = f;
		}, 0.05f, 0.75f, CanvasSettingsApplicator.MaxCanvasScale(), delegate(float f)
		{
			_uiScaleValue = f;
			Preferences.GraphicsCanvasScale = _uiScaleValue;
		}));
		builder.RebuildOnEvent<CanvasScaleChanged>();
		List<string> values2 = new List<string> { "Limit to 60 FPS", "Don't Sync", "Monitor Rate", "1/2 Monitor Rate" };
		builder.AddField("Vsync", builder.AddDropdown(values2, (int)(Preferences.GraphicsVsync + 1), delegate(int vSyncCount)
		{
			Preferences.GraphicsVsync = (Preferences.GraphicsVsyncOption)(vSyncCount - 1);
		}));
		string[] qualityValues = QualitySettings.names;
		List<int> values3 = qualityValues.Select((string _, int i) => i).ToList();
		int qualityLevel = QualitySettings.GetQualityLevel();
		builder.AddField("Quality", builder.AddDropdownIntPicker(values3, qualityLevel, (int i) => qualityValues[i], canWrite: true, delegate(int value)
		{
			QualitySettings.SetQualityLevel(value, applyExpensiveChanges: true);
			builder.Rebuild();
		}));
		Preferences.ParticleLevel[] particleValues = new Preferences.ParticleLevel[2]
		{
			Preferences.ParticleLevel.Off,
			Preferences.ParticleLevel.Standard
		};
		string[] particleNames = new string[2] { "Off", "Standard" };
		List<int> values4 = particleValues.Select((Preferences.ParticleLevel _, int i) => i).ToList();
		int selected2 = particleValues.ToList().FindIndex((Preferences.ParticleLevel pv) => pv == Preferences.GraphicsParticleLevel);
		builder.AddField("Particles", builder.AddDropdownIntPicker(values4, selected2, (int i) => particleNames[i], canWrite: true, delegate(int i)
		{
			Preferences.GraphicsParticleLevel = particleValues[i];
		}));
		builder.AddField("Draw Distance", builder.AddSliderQuantized(() => Preferences.GraphicsDrawDistance, () => $"{Mathf.Round(Preferences.GraphicsDrawDistance / 100f) / 10f:F1}k", delegate(float f)
		{
			Preferences.GraphicsDrawDistance = f;
		}, 100f, 500f, 1500f));
		_pendingTreeDensity = Preferences.GraphicsTreeDensity;
		_pendingDetailDensity = Preferences.GraphicsDetailDensity;
		builder.AddField("Tree Density", builder.AddSliderQuantized(() => _pendingTreeDensity, () => _pendingTreeDensity.ToString("F1"), delegate(float value)
		{
			_pendingTreeDensity = value;
		}, 0.1f, 0f, 2f, delegate(float value)
		{
			_pendingTreeDensity = value;
			ApplyDensityAfterDelay(builder);
		}));
		builder.AddField("Detail Density", builder.AddSliderQuantized(() => _pendingDetailDensity, () => _pendingDetailDensity.ToString("F1"), delegate(float value)
		{
			_pendingDetailDensity = value;
		}, 0.1f, 0f, 1f, delegate(float value)
		{
			_pendingDetailDensity = value;
			ApplyDensityAfterDelay(builder);
		}));
		builder.AddSection("Field of View");
		builder.AddField("Default", builder.AddSlider(() => Preferences.DefaultFOV, () => Preferences.DefaultFOV.ToString("N0"), delegate(float value)
		{
			Preferences.DefaultFOV = value;
		}, 4f, 120f, wholeNumbers: true));
		builder.AddField("Alternate", builder.AddSlider(() => Preferences.AlternateFOV, () => Preferences.AlternateFOV.ToString("N0"), delegate(float value)
		{
			Preferences.AlternateFOV = value;
		}, 4f, 120f, wholeNumbers: true));
		builder.AddSection("Levels");
		builder.AddField("Post Exposure", builder.AddSlider(() => Preferences.GraphicsPostExposure, () => Preferences.GraphicsPostExposure.ToString("N1"), delegate(float value)
		{
			Preferences.GraphicsPostExposure = value;
		}, 0f, 2f));
		builder.AddField("Contrast", builder.AddSlider(() => Preferences.GraphicsContrast, () => Preferences.GraphicsContrast.ToString("N0"), delegate(float value)
		{
			Preferences.GraphicsContrast = value;
		}, 0f, 100f));
		builder.AddExpandingVerticalSpacer();
	}

	private static void ApplyDensityAfterDelay(UIPanelBuilder builder)
	{
		LeanTween.cancel(_applyDensityAfterDelay);
		_applyDensityAfterDelay = LeanTween.delayedCall(0.25f, (Action)delegate
		{
			MapCameraUpdater.SetTerrainDensityValues(_pendingTreeDensity, _pendingDetailDensity);
			builder.Rebuild();
		}).id;
	}

	private static void BuildTabSound(UIPanelBuilder builder)
	{
		builder.Spacing = 0f;
		AddVolumeSlider(0, "Main", () => Preferences.SoundVolumeMain, delegate(float v)
		{
			Preferences.SoundVolumeMain = v;
		});
		AddVolumeSlider(1, "Engine", () => Preferences.SoundVolumeEngine, delegate(float v)
		{
			Preferences.SoundVolumeEngine = v;
		});
		AddVolumeSlider(2, "Whistle", () => Preferences.SoundVolumeWhistle, delegate(float v)
		{
			Preferences.SoundVolumeWhistle = v;
		});
		AddVolumeSlider(2, "Bell", () => Preferences.SoundVolumeBell, delegate(float v)
		{
			Preferences.SoundVolumeBell = v;
		});
		AddVolumeSlider(2, "Dynamo", () => Preferences.SoundVolumeDynamo, delegate(float v)
		{
			Preferences.SoundVolumeDynamo = v;
		});
		AddVolumeSlider(1, "Wheels", () => Preferences.SoundVolumeWheels, delegate(float v)
		{
			Preferences.SoundVolumeWheels = v;
		});
		AddVolumeSlider(1, "Environment", () => Preferences.SoundVolumeEnvironment, delegate(float v)
		{
			Preferences.SoundVolumeEnvironment = v;
		});
		AddVolumeSlider(1, "CTC Bell", () => Preferences.SoundVolumeCtcBell, delegate(float v)
		{
			Preferences.SoundVolumeCtcBell = v;
		});
		builder.AddExpandingVerticalSpacer();
		void AddVolumeSlider(int indent, string name, Func<float> getValue, Action<float> setValue)
		{
			builder.HStack(delegate(UIPanelBuilder uIPanelBuilder)
			{
				uIPanelBuilder.Spacer(indent * 16);
				uIPanelBuilder.AddLabel(name).Width(140 - indent * 16);
				uIPanelBuilder.AddSlider(getValue, () => Mathf.RoundToInt(getValue() * 100f).ToString(), setValue, 0f, 2f).FlexibleWidth();
			}).Height(32f);
		}
	}

	private static void BuildTabFeatures(UIPanelBuilder builder)
	{
		BuildBehaviorSection(builder);
		BuildOtherSection(builder);
		builder.AddExpandingVerticalSpacer();
	}
}
