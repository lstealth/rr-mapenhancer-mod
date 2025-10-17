using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Game;
using Game.Events;
using Game.Messages;
using Game.State;
using Model;
using Model.Ops;
using Serilog;
using TMPro;
using UI.Builder;
using UI.Common;
using UI.SwitchList;
using UnityEngine;

namespace UI.StationWindow;

[RequireComponent(typeof(Window))]
public class StationWindow : MonoBehaviour, IBuilderWindow
{
	private const string TabIdPassenger = "passenger";

	private const string TabIdFreight = "freight";

	private Window _window;

	private static StationWindow _instance;

	private readonly UIState<string> _selectedTabState = new UIState<string>(null);

	private UIPanel _panel;

	private const int HstackSpacing = 8;

	private const int ColumnWidthCar = 130;

	private const int ColumnWidthLocation = 240;

	private const int ColumnWidthToggle = 20;

	public UIBuilderAssets BuilderAssets { get; set; }

	public static StationWindow Shared => WindowManager.Shared.GetWindow<StationWindow>();

	public void Show(string title, List<Area> areas, PassengerStop passengerStop, OpsCarList freightCars)
	{
		Populate(title, areas, passengerStop, freightCars);
		_window.ShowWindow();
	}

	private void Awake()
	{
		_window = GetComponent<Window>();
	}

	private void OnDisable()
	{
		_panel?.Dispose();
		_panel = null;
	}

	private void Populate(string title, List<Area> areas, PassengerStop passengerStop, OpsCarList freightCars)
	{
		_window.Title = title;
		_panel?.Dispose();
		_panel = UIPanel.Create(_window.contentRectTransform, BuilderAssets, delegate(UIPanelBuilder builder)
		{
			builder.AddTabbedPanels(_selectedTabState, delegate(UITabbedPanelBuilder tabBuilder)
			{
				if (passengerStop != null)
				{
					tabBuilder.AddTab("Passengers", "passenger", delegate(UIPanelBuilder b)
					{
						BuildPassengerTab(b, passengerStop);
					});
				}
				if (areas.Count > 0 && freightCars != null)
				{
					tabBuilder.AddTab("Freight", "freight", delegate(UIPanelBuilder b)
					{
						BuildFreightTab(b, areas, freightCars);
					});
				}
			});
		});
	}

	private void BuildFreightTab(UIPanelBuilder builder, List<Area> areas, OpsCarList freightCars)
	{
		if (areas.Count == 0)
		{
			throw new ArgumentOutOfRangeException("areas", "Must be non-empty");
		}
		TrainController trainController = TrainController.Shared;
		PlayersManager playersManager = StateManager.Shared.PlayersManager;
		string text = areas.Select((Area a) => a.name).CommaSeparatedString();
		string text2 = ((areas.Count > 1) ? "have" : "has");
		bool isOnTrainCrew = playersManager.MyTrainCrew != null;
		SwitchListPanel switchListPanel = SwitchListPanel.Shared;
		builder.AddLabel(text + " " + text2 + " " + freightCars.Entries.Count.Pluralize("car") + " with unfulfilled waybills.");
		builder.Spacer(8f);
		if (isOnTrainCrew)
		{
			builder.AddLabel("Add/remove cars from your train crew's switch list using the toggle.");
		}
		else
		{
			builder.AddLabel("<i>Join a Train Crew to add cars to your switch list.</i>");
		}
		builder.Spacer(8f);
		builder.HStack(delegate(UIPanelBuilder uIPanelBuilder)
		{
			uIPanelBuilder.AddLabel("<b>Car</b>").Width(130f);
			uIPanelBuilder.AddLabel("<b>Location</b>").Width(240f);
			uIPanelBuilder.AddLabel("<b>Destination</b>").FlexibleWidth(1f);
			uIPanelBuilder.AddLabel("<b>Switch List</b>", delegate(TMP_Text t)
			{
				t.textWrappingMode = TextWrappingModes.NoWrap;
				t.alignment = TextAlignmentOptions.Right;
			}).Width(20f);
		}, 8f);
		builder.VScrollView(delegate(UIPanelBuilder uIPanelBuilder)
		{
			uIPanelBuilder.RebuildOnEvent<SwitchListDidChange>();
			foreach (OpsCarList.Entry entry in freightCars.Entries)
			{
				Car car = trainController.CarForId(entry.CarId);
				if (car == null)
				{
					break;
				}
				uIPanelBuilder.HStack(delegate(UIPanelBuilder builder2)
				{
					bool isOnSwitchList = switchListPanel.SwitchListContains(entry.CarId);
					BuildFreightCarRow(builder2, car.CarType + " " + car.DisplayName, entry, isOnTrainCrew, isOnSwitchList, delegate(bool on)
					{
						StateManager.ApplyLocal(new SwitchListToggleCarIds(playersManager.MyTrainCrew.Id, new List<string> { car.id }, on));
					});
				}, 8f);
			}
		}, new RectOffset(0, 4, 0, 0));
	}

	private static void BuildFreightCarRow(UIPanelBuilder builder, string displayName, OpsCarList.Entry entry, bool isOnTrainCrew, bool isOnSwitchList, Action<bool> toggle)
	{
		builder.AddLabel(displayName, delegate(TMP_Text text)
		{
			text.textWrappingMode = TextWrappingModes.NoWrap;
			text.overflowMode = TextOverflowModes.Ellipsis;
		}).Width(130f);
		Car car = TrainController.Shared.CarForId(entry.CarId);
		if (car == null)
		{
			Log.Error("Failed to find car: {carId}", entry.CarId);
			return;
		}
		if (entry.Current.IsConcrete)
		{
			builder.AddLocationField(industryTrackDisplayable: new OpsCarListEntryDisplayable(entry.Current), locationName: entry.Current.Title, jump: delegate
			{
				CameraSelector.shared.FollowCar(car);
			}).Width(240f);
		}
		else
		{
			string icName = "<i>" + entry.Current.Title + "</i>";
			builder.AddLocationFieldFallback(icName, delegate
			{
				CameraSelector.shared.FollowCar(car);
			}).Width(240f);
		}
		OpsCarListEntryDisplayable destination = new OpsCarListEntryDisplayable(entry.Destination);
		builder.AddLocationField(entry.Destination.Title, destination, delegate
		{
			CameraSelector.shared.JumpTo(destination);
		}).FlexibleWidth(1f);
		if (isOnTrainCrew)
		{
			builder.AddToggle(() => isOnSwitchList, toggle).Tooltip("On Switch List", "Toggle whether this car is on your train crew's switch list.").Width(20f);
		}
		else
		{
			builder.AddLabel("-").Width(20f);
		}
	}

	private void BuildPassengerTab(UIPanelBuilder builder, PassengerStop passengerStop)
	{
		builder.VScrollView(delegate(UIPanelBuilder uIPanelBuilder)
		{
			uIPanelBuilder.RebuildOnInterval(1f);
			IReadOnlyDictionary<string, PassengerStop.WaitingInfo> waiting = passengerStop.Waiting;
			IEnumerable<KeyValuePair<string, PassengerStop.WaitingInfo>> enumerable = waiting.Where((KeyValuePair<string, PassengerStop.WaitingInfo> pair) => pair.Value.Total > 0);
			bool flag = waiting.Count == 0;
			int number = waiting.Sum((KeyValuePair<string, PassengerStop.WaitingInfo> kv) => kv.Value.Total);
			string text = (flag ? "no passengers" : number.Pluralize("passenger"));
			uIPanelBuilder.AddLabel(passengerStop.DisplayName + " has " + text + " waiting.");
			uIPanelBuilder.Spacer(8f);
			if (flag)
			{
				return;
			}
			uIPanelBuilder.HStack(delegate(UIPanelBuilder uIPanelBuilder2)
			{
				uIPanelBuilder2.AddLabel("<b>Count</b>").Width(80f);
				uIPanelBuilder2.AddLabel("<b>Destination</b>").Width(200f);
				uIPanelBuilder2.AddLabel("<b>Longest Wait</b>");
			});
			GameDateTime now = TimeWeather.Now;
			foreach (KeyValuePair<string, PassengerStop.WaitingInfo> item in enumerable)
			{
				item.Deconstruct(out var key, out var value);
				string identifier = key;
				PassengerStop.WaitingInfo waiting2 = value;
				string destName = PassengerStop.NameForIdentifier(identifier);
				uIPanelBuilder.HStack(delegate(UIPanelBuilder uIPanelBuilder2)
				{
					uIPanelBuilder2.AddLabel($"{waiting2.Total}").Width(80f);
					uIPanelBuilder2.AddLabel(destName).Width(200f);
					GameDateTime gameDateTime = new GameDateTime(3.4028234663852886E+38);
					GameDateTime gameDateTime2 = GameDateTime.Zero;
					foreach (WaitingPassengerGroup group in waiting2.Groups)
					{
						if (!(group.Origin == passengerStop.identifier))
						{
							if (group.Boarded < gameDateTime)
							{
								gameDateTime = group.Boarded;
							}
							if (group.Boarded > gameDateTime2)
							{
								gameDateTime2 = group.Boarded;
							}
						}
					}
					uIPanelBuilder2.AddLabel((gameDateTime2.TotalSeconds == 0.0) ? "N/A" : (gameDateTime.IntervalString(now, GameDateTimeInterval.Style.Short) ?? ""));
				});
			}
		});
	}
}
