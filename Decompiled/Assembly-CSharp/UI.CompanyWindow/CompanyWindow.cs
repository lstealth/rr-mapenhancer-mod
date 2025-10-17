using System.Text;
using Game.State;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace UI.CompanyWindow;

[RequireComponent(typeof(Window))]
public class CompanyWindow : MonoBehaviour, IBuilderWindow
{
	private const string TabIdRailroad = "railroad";

	private const string TabIdMilestones = "milestones";

	private const string TabIdFinance = "finance";

	private const string TabIdLocations = "locations";

	private const string TabIdEquipment = "equipment";

	private const string TabIdEmployees = "employees";

	private const string TabIdCrews = "crews";

	private const string TabIdSettings = "settings";

	private Window _window;

	private static CompanyWindow _instance;

	private readonly UIState<string> _selectedTabState = new UIState<string>(null);

	private readonly UIState<string> _selectedGoalsItem = new UIState<string>(null);

	private readonly UIState<string> _selectedLocationsItem = new UIState<string>(null);

	private readonly UIState<string> _selectedSettingsItem = new UIState<string>(null);

	private readonly UIState<string> _selectedCarItem = new UIState<string>(null);

	private readonly UIState<string> _selectedTrainCrewId = new UIState<string>(null);

	private readonly UIState<string> _selectedPlayerId = new UIState<string>(null);

	private UIPanel _panel;

	public UIBuilderAssets BuilderAssets { get; set; }

	public static CompanyWindow Shared => WindowManager.Shared.GetWindow<CompanyWindow>();

	public string ShownPath
	{
		get
		{
			Window window = _window;
			if ((object)window == null || !window.IsShown)
			{
				return null;
			}
			StringBuilder stringBuilder = new StringBuilder();
			string value = _selectedTabState.Value;
			stringBuilder.Append(value);
			if (value == "locations")
			{
				stringBuilder.Append("/");
				stringBuilder.Append(_selectedLocationsItem.Value);
			}
			return stringBuilder.ToString();
		}
	}

	public static void Toggle()
	{
		if (Shared._window.IsShown)
		{
			Shared._window.CloseWindow();
		}
		else
		{
			Shared.Show();
		}
	}

	private void Show()
	{
		Populate();
		_window.ShowWindow();
	}

	public void ShowTimeSettings()
	{
		_selectedTabState.Value = "settings";
		_selectedSettingsItem.Value = "time";
		Show();
	}

	public void ShowFinance()
	{
		_selectedTabState.Value = "finance";
		Show();
	}

	private void Awake()
	{
		_window = GetComponent<Window>();
		_window.OnShownDidChange += WindowShownDidChange;
	}

	private void OnDisable()
	{
		_panel?.Dispose();
		_panel = null;
	}

	private void Populate()
	{
		_window.Title = StateManager.Shared.RailroadName;
		_panel?.Dispose();
		_panel = UIPanel.Create(_window.contentRectTransform, BuilderAssets, delegate(UIPanelBuilder builder)
		{
			StateManager stateManager = StateManager.Shared;
			builder.AddObserver(stateManager.Storage.ObserveTimetableFeature(delegate
			{
				builder.Rebuild();
			}, callInitial: false));
			builder.AddTabbedPanels(_selectedTabState, delegate(UITabbedPanelBuilder tabBuilder)
			{
				tabBuilder.AddTab("Railroad", "railroad", RailroadPanelBuilder.Build);
				tabBuilder.AddTab("Locations", "locations", delegate(UIPanelBuilder b)
				{
					LocationsPanelBuilder.Build(b, _selectedLocationsItem);
				});
				if (stateManager.GameMode == GameMode.Company)
				{
					tabBuilder.AddTab("Milestones", "milestones", delegate(UIPanelBuilder b)
					{
						GoalsPanelBuilder.Build(b, _selectedGoalsItem);
					});
				}
				tabBuilder.AddTab("Finance", "finance", FinancePanelBuilder.Build);
				tabBuilder.AddTab("Equipment", "equipment", delegate(UIPanelBuilder b)
				{
					EquipmentPanelBuilder.Build(b, _selectedCarItem);
				});
				tabBuilder.AddTab("Employees", "employees", delegate(UIPanelBuilder b)
				{
					EmployeesPanelBuilder.Build(b, _selectedPlayerId);
				});
				tabBuilder.AddTab("Crews", "crews", delegate(UIPanelBuilder b)
				{
					CrewsPanelBuilder.Build(b, _selectedTrainCrewId);
				});
				tabBuilder.AddTab("Settings", "settings", delegate(UIPanelBuilder b)
				{
					SettingsPanelBuilder.Build(b, _selectedSettingsItem);
				});
			});
		});
	}

	private void WindowShownDidChange(bool shown)
	{
		if (!shown)
		{
			_selectedLocationsItem.Value = null;
		}
	}

	public void ShowIndustry(string industryId)
	{
		_selectedTabState.Value = "locations";
		_selectedLocationsItem.Value = industryId;
		Show();
	}

	public void ShowPlayer(string playerId)
	{
		_selectedTabState.Value = "employees";
		_selectedPlayerId.Value = playerId;
		Show();
	}

	public void ShowCrew(string trainCrewId)
	{
		_selectedTabState.Value = "crews";
		_selectedTrainCrewId.Value = trainCrewId;
		Show();
	}
}
