using System;
using Game.Scripting.Interactive;
using UI.Builder;
using UI.CarCustomizeWindow;
using UI.CarInspector;
using UI.Common;
using UI.CompanyWindow;
using UI.Equipment;
using UI.Guide;
using UI.LostCarPlacer;
using UI.PreferencesWindow;
using UI.StationWindow;
using UI.Timetable;
using UnityEngine;

namespace UI;

public class ProgrammaticWindowCreator : MonoBehaviour
{
	public Window windowPrefab;

	public UIBuilderAssets builderAssets;

	private void Start()
	{
		CreateWindow<UI.CompanyWindow.CompanyWindow>("Company", 880, 600, Window.Position.Center);
		CreateWindow<UI.PreferencesWindow.PreferencesWindow>("Preferences", 400, 400, Window.Position.Center, Window.Sizing.Resizable(new Vector2Int(400, 400), new Vector2Int(600, 800)));
		CreateWindow<BindingsWindow>("Bindings", 500, 500, Window.Position.Center);
		CreateWindow<UI.CarInspector.CarInspector>("CarInspector", 400, 320, Window.Position.LowerRight);
		CreateWindow<UI.CarCustomizeWindow.CarCustomizeWindow>("CarCustomize", 400, 320, Window.Position.Center);
		CreateWindow<LostCarPlacerWindow>("LostCarPlacer", 400, 320, Window.Position.Center);
		CreateWindow<GuideWindow>("Guide", 900, 600, Window.Position.Center, Window.Sizing.Resizable(new Vector2Int(600, 400), new Vector2Int(1200, 1200)));
		CreateWindow<UI.StationWindow.StationWindow>("Station", 700, 500, Window.Position.Center);
		CreateWindow<TimeWindow>();
		CreateWindow<TimetableWindow>();
		CreateWindow<TimetableEditorWindow>();
		CreateWindow<EquipmentWindow>("Equipment", 800, 600, Window.Position.Center);
		CreateWindow<InteractiveBookWindow>();
	}

	private void CreateWindow<TWindow>(string identifier, int width, int height, Window.Position position, Window.Sizing sizing = default(Window.Sizing), Action<TWindow> configure = null) where TWindow : Component, IBuilderWindow
	{
		if (sizing.Equals(default(Window.Sizing)))
		{
			sizing = Window.Sizing.Fixed(new Vector2Int(width, height));
		}
		Window window = CreateWindow();
		window.SetInitialPositionSize(identifier, new Vector2(width, height), position, sizing);
		window.name = typeof(TWindow).ToString();
		TWindow val = window.gameObject.AddComponent<TWindow>();
		val.BuilderAssets = builderAssets;
		window.CloseWindow();
		configure?.Invoke(val);
	}

	private void CreateWindow<TWindow>(Action<TWindow> configure = null) where TWindow : Component, IProgrammaticWindow
	{
		Window window = CreateWindow();
		window.name = typeof(TWindow).ToString();
		TWindow val = window.gameObject.AddComponent<TWindow>();
		val.BuilderAssets = builderAssets;
		window.CloseWindow();
		window.SetInitialPositionSize(val.WindowIdentifier, val.DefaultSize, val.DefaultPosition, val.Sizing);
		configure?.Invoke(val);
	}

	private Window CreateWindow()
	{
		return UnityEngine.Object.Instantiate(windowPrefab, base.transform, worldPositionStays: false);
	}
}
