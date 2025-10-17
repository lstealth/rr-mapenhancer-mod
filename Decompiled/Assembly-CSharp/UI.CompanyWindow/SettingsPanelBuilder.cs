using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Game;
using Game.AccessControl;
using Game.Messages;
using Game.Progression;
using Game.State;
using Model;
using Model.AI;
using Network;
using Serilog;
using TMPro;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace UI.CompanyWindow;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct SettingsPanelBuilder
{
	private enum PageId
	{
		Time,
		MultiplayerAccessControl,
		MapFeatures,
		Features
	}

	private class Page
	{
		public PageId Id { get; }

		public Page(PageId id)
		{
			Id = id;
		}
	}

	private static readonly List<int> PassengerCountValues = new List<int> { 0, 1, 2, 4, 8, 16, 32 };

	private static bool ShouldShowMapFeatures
	{
		get
		{
			if (StateManager.IsSandbox)
			{
				return StateManager.IsHost;
			}
			return false;
		}
	}

	public static void Build(UIPanelBuilder builder, UIState<string> selectedItem)
	{
		if (selectedItem.Value == null)
		{
			selectedItem.Value = "time";
		}
		List<UIPanelBuilder.ListItem<Page>> list = new List<UIPanelBuilder.ListItem<Page>>();
		list.Add(new UIPanelBuilder.ListItem<Page>("time", new Page(PageId.Time), "General", "Time"));
		list.Add(new UIPanelBuilder.ListItem<Page>("features", new Page(PageId.Features), "General", "Features"));
		if (StateManager.IsHost)
		{
			list.Add(new UIPanelBuilder.ListItem<Page>("mpac", new Page(PageId.MultiplayerAccessControl), "Multiplayer", "Access Control"));
		}
		if (ShouldShowMapFeatures)
		{
			list.Add(new UIPanelBuilder.ListItem<Page>("mapFeatures", new Page(PageId.MapFeatures), "Advanced", "Map Features"));
		}
		builder.AddListDetail(list, selectedItem, delegate(UIPanelBuilder uIPanelBuilder, Page page)
		{
			if (page == null)
			{
				uIPanelBuilder.AddExpandingVerticalSpacer();
				uIPanelBuilder.AddLabelEmptyState("Select a page");
				uIPanelBuilder.AddExpandingVerticalSpacer();
			}
			else
			{
				uIPanelBuilder.VScrollView(delegate(UIPanelBuilder builder2)
				{
					switch (page.Id)
					{
					case PageId.Time:
						BuildTime(builder2);
						break;
					case PageId.Features:
						BuildFeatures(builder2);
						break;
					case PageId.MapFeatures:
						BuildMapFeatures(builder2);
						break;
					case PageId.MultiplayerAccessControl:
						BuildMultiplayerAccessControl(builder2);
						break;
					default:
						builder2.AddLabel("Unknown page.");
						break;
					}
				}, new RectOffset(0, 4, 0, 0));
			}
		});
	}

	private static void BuildTime(UIPanelBuilder builder)
	{
		StateManager shared = StateManager.Shared;
		GameStorage gameStorage = shared.Storage;
		builder.AddSection("Time", delegate(UIPanelBuilder builder2)
		{
			builder2.AddField("Time of Day", () => TimeWeather.TimeOfDayString, UIPanelBuilder.Frequency.Fast);
			if (StateManager.CheckAuthorizedToSendMessage(default(WaitTime)))
			{
				builder2.AddField(null, builder2.AddButton("Open Time Window", delegate
				{
					TimeWindow.Shared.Show();
				}).RectTransform);
			}
			builder2.AddObserver(gameStorage.ObserveTimeMultiplier(delegate
			{
				builder2.Rebuild();
			}, initial: false));
			bool canWrite = StateManager.CheckAuthorizedToChangeProperty("_game", "interchangeServeHour");
			int interchangeServeHour = gameStorage.InterchangeServeHour;
			List<int> values = new List<int> { 4, 5, 6, 7, 8, 9, 10 };
			builder2.AddField("Interchange Served", builder2.AddDropdownIntPicker(values, interchangeServeHour, (int hh) => new GameDateTime(0, hh).TimeString(), canWrite, delegate(int newValue)
			{
				gameStorage.InterchangeServeHour = newValue;
			})).Tooltip("Interchange Service Time", "Time of day that the interchange will first be served.");
		}, 8f);
		builder.AddExpandingVerticalSpacer();
	}

	private static void BuildFeatures(UIPanelBuilder builder)
	{
		builder.AddSection("Interchange");
		BuildFeatureInterchangeBlocking(builder);
		builder.AddSection("Simulation");
		BuildFeatureBrakeForce(builder);
		builder.AddSection("Wear & Tear");
		BuildFeatureWear(builder);
		builder.AddSection("Auto Engineer (AI)");
		BuildFeatureAICrossings(builder);
		BuildFeatureAICallSignals(builder);
		BuildFeatureAIPassengerStop(builder);
		builder.AddSection("Operations");
		BuildFeatureTimetable(builder);
		builder.AddSection("Map");
		BuildFeatureMap(builder);
		builder.AddExpandingVerticalSpacer();
	}

	private static void BuildFeatureInterchangeBlocking(UIPanelBuilder builder)
	{
		StateManager shared = StateManager.Shared;
		GameStorage gameStorage = shared.Storage;
		List<int> values = new List<int> { 0, 1, 3, 5 };
		Dictionary<int, string> interchangeShuffleStrings = new Dictionary<int, string>
		{
			{ 0, "Blocked by Destination" },
			{ 1, "Mostly Blocked" },
			{ 3, "Somewhat Blocked" },
			{ 5, "Hostile" }
		};
		builder.AddField("Blocking", builder.AddDropdownIntPicker(values, gameStorage.InterchangeShuffle, (int i) => (!interchangeShuffleStrings.TryGetValue(i, out var value)) ? $"Shuffle {i}" : value, GameStorage.CanWriteInterchangeShuffle, delegate(int i)
		{
			gameStorage.InterchangeShuffle = i;
		})).Tooltip("Interchange Blocking", "Determines the degree to which cars are delivered to the interchange blocked (grouped) by their destination.");
	}

	private static void BuildFeatureBrakeForce(UIPanelBuilder builder)
	{
		StateManager stateManager = StateManager.Shared;
		builder.AddField("Braking Force", MakeBrakeForceDropdown()).Tooltip("Braking Force", "Higher braking force makes trains easier to stop.");
		if (Car.BrakeForceMultiplier < 1f)
		{
			builder.AddField(null, "<i>Warning: Auto Engineers are tuned for Medium braking force. Expect derailments and hard couplings!</i>");
		}
		RectTransform MakeBrakeForceDropdown()
		{
			float defaultBrakeForce = TrainController.Shared.config.brakeForceMultiplier;
			float brakeForceMultiplier = Car.BrakeForceMultiplier;
			Dictionary<int, string> brakeForceOptions = new Dictionary<int, string>
			{
				{ 50, "Low" },
				{ 100, "Medium" }
			};
			List<int> values = brakeForceOptions.Keys.OrderBy((int i) => i).ToList();
			string value;
			return builder.AddDropdownIntPicker(values, Mathf.RoundToInt(brakeForceMultiplier * 100f), (int i) => (!brakeForceOptions.TryGetValue(i, out value)) ? $"{i}%" : value, GameStorage.CanWriteBrakeForce, delegate(int i)
			{
				bool flag = i == Mathf.RoundToInt(defaultBrakeForce * 100f);
				stateManager.Storage.BrakeForce = (flag ? ((float?)null) : new float?((float)i / 100f));
				Log.Debug("Set BrakeForce to {value}", stateManager.Storage.BrakeForce);
				builder.Rebuild();
			});
		}
	}

	private static void BuildFeatureWear(UIPanelBuilder builder)
	{
		GameStorage gameStorage = StateManager.Shared.Storage;
		builder.AddObserver(gameStorage.ObserveWearFeature(delegate
		{
			builder.Rebuild();
		}, observeFirst: false));
		builder.AddField("Wear & Tear", builder.AddToggle(() => gameStorage.WearFeature, delegate(bool value)
		{
			gameStorage.WearFeature = value;
			if (gameStorage.OilFeature)
			{
				RequestSaveReopen();
			}
		})).Tooltip("Wear, Tear & Overhaul", "Equipment takes gradual wear when moved and requires periodic overhaul repair service.");
		if (!gameStorage.WearFeature)
		{
			return;
		}
		builder.AddField("Wear Rate", builder.AddSliderQuantized(() => gameStorage.WearMultiplier, () => $"{gameStorage.WearMultiplier * 100f:F0}%", delegate(float value)
		{
			gameStorage.WearMultiplier = value;
		}, 0.1f, 0.1f, 5f)).Tooltip("Wear Rate", "Affects the rate at which equipment condition decreases with movement.");
		builder.AddField("Overhaul Miles", builder.AddSliderQuantized(() => gameStorage.OverhaulMiles, () => $"{(float)gameStorage.OverhaulMiles / 1000f:N1}k", delegate(float value)
		{
			gameStorage.OverhaulMiles = Mathf.RoundToInt(value);
		}, 100f, 500f, 10000f)).Tooltip("Miles Between Overhauls", "Number of miles before an Overhaul is required. If an overhaul is not performed in time, the repair shop will be unable to repair equipment to 100%.");
		builder.AddField("Hotboxes & Oiling", builder.AddToggle(() => gameStorage.OilFeature, delegate(bool value)
		{
			gameStorage.OilFeature = value;
			RequestSaveReopen();
			builder.Rebuild();
		})).Tooltip("Hotboxes & Oiling", "Bearings on equipment must be regularly oiled or it will incur additional running wear. Hotboxes may occur when running with equipment that is not adequately oiled.");
		if (gameStorage.OilFeature)
		{
			builder.AddField("Oil Use Rate", builder.AddSliderQuantized(() => gameStorage.OilUseMultiplier, () => $"{gameStorage.OilUseMultiplier * 100f:F0}%", delegate(float value)
			{
				gameStorage.OilUseMultiplier = value;
			}, 0.1f, 0.1f, 5f)).Tooltip("Oil Use Rate", "Affects the rate oil is used when equipment moves.");
		}
	}

	private static void RequestSaveReopen()
	{
		ModalAlertController.PresentOkay("Reload Required", "Please save and reopen your game to apply changes.");
	}

	private static void BuildFeatureAICrossings(UIPanelBuilder builder)
	{
		StateManager stateManager = StateManager.Shared;
		builder.AddField("Crossing Signal", MakeDropdown()).Tooltip("Crossing Signal", "Controls AI behavior when approaching a public crossing.");
		RectTransform MakeDropdown()
		{
			return builder.AddToggle(() => stateManager.Storage.AICrossingSignal != CrossingSignalSetting.Off, delegate(bool en)
			{
				stateManager.Storage.AICrossingSignal = (en ? CrossingSignalSetting.On : CrossingSignalSetting.Off);
			});
		}
	}

	private static void BuildFeatureAICallSignals(UIPanelBuilder builder)
	{
		StateManager stateManager = StateManager.Shared;
		builder.AddField("Call Signals", MakeDropdown()).Tooltip("Call Signals", "Auto Engineer will call non-clear signals.");
		RectTransform MakeDropdown()
		{
			return builder.AddToggle(() => stateManager.Storage.AICallSignals != 0, delegate(bool en)
			{
				stateManager.Storage.AICallSignals = (en ? 1 : 0);
			});
		}
	}

	private static void BuildFeatureAIPassengerStop(UIPanelBuilder builder)
	{
		StateManager shared = StateManager.Shared;
		GameStorage gameStorage = shared.Storage;
		builder.AddField("Passenger Stops", builder.AddToggle(() => gameStorage.AIPassengerStopEnable, delegate(bool en)
		{
			gameStorage.AIPassengerStopEnable = en;
		})).Tooltip("Automatic Passenger Stops", "When enabled, AI engineer makes stops for checked passenger stops in cars within its train.");
		builder.AddField("Minimum Stop Duration", builder.AddSlider(() => (float)gameStorage.AIPassengerStopMinimumStopDuration / 30f, () => SecondsToMMSS(gameStorage.AIPassengerStopMinimumStopDuration), delegate(float en)
		{
			gameStorage.AIPassengerStopMinimumStopDuration = Mathf.RoundToInt(en * 30f);
		}, 1f, 60f, wholeNumbers: true)).Tooltip("Minimum Stop Duration", "Minimum game time that the AI engineer will stop at a station before continuing.");
		static string SecondsToMMSS(int seconds)
		{
			return $"{seconds / 60}:{seconds % 60:D2}";
		}
	}

	private static void BuildFeatureTimetable(UIPanelBuilder builder)
	{
		StateManager shared = StateManager.Shared;
		GameStorage gameStorage = shared.Storage;
		builder.AddField("Timetable", builder.AddToggle(() => gameStorage.TimetableFeature, delegate(bool en)
		{
			gameStorage.TimetableFeature = en;
		})).Tooltip("Timetable", "");
	}

	private static void BuildFeatureMap(UIPanelBuilder builder)
	{
		StateManager shared = StateManager.Shared;
		GameStorage gameStorage = shared.Storage;
		builder.AddField("Show Switches", builder.AddToggle(() => gameStorage.MapShowsSwitches, delegate(bool en)
		{
			gameStorage.MapShowsSwitches = en;
		})).Tooltip("Map Shows Switches", "Show switches on the map. Switches are remotely throw-able.");
	}

	private static void BuildMapFeatures(UIPanelBuilder builder)
	{
		MapFeatureManager mapFeatureManager = MapFeatureManager.Shared;
		builder.AddObserver(mapFeatureManager.ObserveFeaturesChanged(((UIPanelBuilder)builder).Rebuild, callInitial: false));
		foreach (MapFeature feature in mapFeatureManager.AvailableFeatures)
		{
			builder.HStack(delegate(UIPanelBuilder builder2)
			{
				bool unlocked = feature.Unlocked;
				string displayName = feature.DisplayName;
				BuildMapFeatureRow(builder2, displayName, unlocked, delegate(bool on)
				{
					mapFeatureManager.SetFeatureEnabled(feature, on);
				});
			});
		}
	}

	private static void BuildMapFeatureRow(UIPanelBuilder builder, string displayName, bool isOnSwitchList, Action<bool> toggle)
	{
		builder.AddToggle(() => isOnSwitchList, toggle).Tooltip("Enabled", "Toggle whether this feature is enabled.").Width(20f);
		builder.AddLabel(displayName, delegate(TMP_Text text)
		{
			text.textWrappingMode = TextWrappingModes.NoWrap;
			text.overflowMode = TextOverflowModes.Ellipsis;
		}).FlexibleWidth(1f);
	}

	private static void BuildMultiplayerAccessControl(UIPanelBuilder builder)
	{
		BuildMultiplayerHostSection(builder);
		builder.AddExpandingVerticalSpacer();
	}

	private static void BuildMultiplayerHostSection(UIPanelBuilder builder)
	{
		StateManager stateManager = StateManager.Shared;
		GameStorage gameStorage = stateManager.Storage;
		if (Multiplayer.Mode != ConnectionMode.MultiplayerServer)
		{
			builder.Spacer(4f);
			builder.AddLabel("<b>Server is not open</b>; settings will be applied for future sessions.");
			builder.Spacer(4f);
		}
		builder.AddSection("Multiplayer", delegate(UIPanelBuilder uIPanelBuilder)
		{
			uIPanelBuilder.AddField("Reporting Mark", stateManager.RailroadMark).Tooltip("Reporting Mark", "Other players can find your railroad by this string.");
			uIPanelBuilder.AddField("Log Rejected", uIPanelBuilder.AddToggle(() => Preferences.HostAuthLogging, delegate(bool value)
			{
				Preferences.HostAuthLogging = value;
				uIPanelBuilder.Rebuild();
			})).Tooltip("Log Rejected", "If checked, reasons for rejected connections are logged to the host's console.");
		});
		builder.AddSection("Multiplayer: New Employees", delegate(UIPanelBuilder uIPanelBuilder)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (gameStorage.AllowNewPlayers)
			{
				stringBuilder.Append("New employees can connect");
				if (!string.IsNullOrEmpty(gameStorage.NewPlayerPasswordHash))
				{
					stringBuilder.Append(" and must supply a password.");
				}
				else
				{
					stringBuilder.Append(" without a password.");
				}
				stringBuilder.Append($" New employees will be granted <b>{gameStorage.DefaultAccessLevel}</b>.");
			}
			else
			{
				stringBuilder.Append("Unknown players can not connect.");
			}
			uIPanelBuilder.AddField("Summary", stringBuilder.ToString());
			uIPanelBuilder.Spacer(36f);
			uIPanelBuilder.AddField("Allow", uIPanelBuilder.AddToggle(() => gameStorage.AllowNewPlayers, delegate(bool value)
			{
				gameStorage.AllowNewPlayers = value;
				Multiplayer.UpdateLobbyFlags();
				uIPanelBuilder.Rebuild();
			})).Tooltip("Allow New Employees", "If checked, new employees (players) may join the server. Existing players can always join unless banned.");
			uIPanelBuilder.AddObserver(gameStorage.ObserveNewPlayerPasswordHash(((UIPanelBuilder)uIPanelBuilder).Rebuild, initial: false));
			RectTransform control = ((!string.IsNullOrEmpty(gameStorage.NewPlayerPasswordHash)) ? uIPanelBuilder.AddInputField("********", SetNewPlayerPassword) : uIPanelBuilder.AddInputField("", SetNewPlayerPassword));
			uIPanelBuilder.AddField("Password", control).Tooltip("New Player Password", "If not empty, new players must supply the password to join. Existing, unbanned players do not need the password.");
			BuildDefaultAccessLevelSetting(uIPanelBuilder);
		});
		builder.AddSection("Multiplayer: Passengers", delegate(UIPanelBuilder builder2)
		{
			builder2.AddField("Limit Number", builder2.AddDropdownIntPicker(PassengerCountValues, gameStorage.PassengerLimit, (int n) => n.ToString(), canWrite: true, delegate(int n)
			{
				gameStorage.PassengerLimit = n;
			})).Tooltip("Passenger Limit", "Maximum number of passengers allowed to connect.");
		});
		builder.AddSection("Multiplayer: Train Crew Access", delegate(UIPanelBuilder uIPanelBuilder)
		{
			uIPanelBuilder.AddFieldToggle("Trainmaster Manages Crew Assignments", () => gameStorage.TrainCrewMembershipManagedByTrainmaster, delegate(bool value)
			{
				gameStorage.TrainCrewMembershipManagedByTrainmaster = value;
			}).Tooltip("Trainmaster Manages Crew Assignments", "Crew players can not join Train Crews themselves; Trainmaster and up must assign.");
			uIPanelBuilder.AddFieldToggle("Restrict Equipment Control to Train Crew", () => gameStorage.TrainCrewMembershipRequired, delegate(bool value)
			{
				gameStorage.TrainCrewMembershipRequired = value;
			}).Tooltip("Restrict Equipment Control to Train Crew", "Equipment which is assigned to a Train Crew may only be controlled by members of that Train Crew.");
		});
		void SetNewPlayerPassword(string str)
		{
			gameStorage.SetNewPlayerPassword(str);
			Multiplayer.UpdateLobbyFlags();
		}
	}

	private static void BuildDefaultAccessLevelSetting(UIPanelBuilder builder)
	{
		AccessLevel[] accessLevelOptions = new AccessLevel[5]
		{
			AccessLevel.Passenger,
			AccessLevel.Crew,
			AccessLevel.Dispatcher,
			AccessLevel.Trainmaster,
			AccessLevel.Officer
		};
		GameStorage gameStorage = StateManager.Shared.Storage;
		AccessLevel defaultAccessLevel = gameStorage.DefaultAccessLevel;
		List<string> values = accessLevelOptions.Select((AccessLevel al) => al.ToString()).ToList();
		int currentSelectedIndex = accessLevelOptions.ToList().IndexOf(defaultAccessLevel);
		builder.AddField("Role", builder.AddDropdown(values, currentSelectedIndex, delegate(int newIndex)
		{
			gameStorage.DefaultAccessLevel = accessLevelOptions[newIndex];
			builder.Rebuild();
		})).Tooltip("New Employee Role", "New employees will be given this role upon connecting.");
	}

	private static void Wait(float hours)
	{
		StateManager.ApplyLocal(new WaitTime
		{
			Hours = hours
		});
	}
}
