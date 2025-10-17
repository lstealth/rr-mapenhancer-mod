using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core;
using GalaSoft.MvvmLight.Messaging;
using Game;
using Game.Events;
using Game.Messages;
using Game.State;
using JetBrains.Annotations;
using Model;
using Model.Definition;
using Model.Ops;
using Model.Ops.Timetable;
using Serilog;
using UI.Builder;
using UI.CarCustomizeWindow;
using UI.Common;
using UI.CompanyWindow;
using UI.SwitchList;
using UnityEngine;

namespace UI.CarInspector;

[RequireComponent(typeof(Window))]
public class CarInspector : MonoBehaviour, IBuilderWindow
{
	private Window _window;

	private Car _car;

	private List<TrainCrew> _trainCrews;

	private static CarInspector _instance;

	private Dictionary<string, PassengerStop> _stopsLookup;

	private readonly HashSet<IDisposable> _observers = new HashSet<IDisposable>();

	private PassengerMarker? _cachedMarker;

	private readonly UIState<string> _selectedTabState = new UIState<string>(null);

	private const float OpsPanelFieldLabelWidth = 100f;

	public UIBuilderAssets BuilderAssets { get; set; }

	private static bool IsSandbox => StateManager.IsSandbox;

	private Waybill? Waybill => _car.GetWaybill(OpsController.Shared);

	private bool CanSetWaybill
	{
		get
		{
			if (!IsSandbox)
			{
				return _car.IsOwnedByPlayer;
			}
			return true;
		}
	}

	public static void Show(Car car)
	{
		if (_instance == null)
		{
			_instance = UnityEngine.Object.FindObjectOfType<CarInspector>();
		}
		_instance.Populate(car);
		_instance._window.ShowWindow();
	}

	internal static Car ShownCar()
	{
		if (_instance == null)
		{
			return null;
		}
		if (!_instance._window.IsShown)
		{
			return null;
		}
		return _instance._car;
	}

	private void Awake()
	{
		_window = GetComponent<Window>();
	}

	private void OnEnable()
	{
		Messenger.Default.Register(this, delegate(CarIdentChanged evt)
		{
			if (_car != null && _car.id == evt.CarId)
			{
				Rebuild();
			}
		});
	}

	private void OnDisable()
	{
		Messenger.Default.Unregister(this);
	}

	private void Rebuild()
	{
		if (!(_car == null))
		{
			Populate(_car);
		}
	}

	private void Populate(Car car)
	{
		if (_car != car)
		{
			_selectedTabState.Value = null;
		}
		_car = car;
		foreach (IDisposable observer in _observers)
		{
			observer?.Dispose();
		}
		_observers.Clear();
		_window.Title = WindowTitleForCar(_car);
		UIPanel.Create(_window.contentRectTransform, BuilderAssets, PopulatePanel);
	}

	private void PopulatePanel(UIPanelBuilder builder)
	{
		builder.AddTitle(TitleForCar(_car), SubtitleForCar(_car));
		builder.AddTabbedPanels(_selectedTabState, delegate(UITabbedPanelBuilder tabBuilder)
		{
			tabBuilder.AddTab("Car", "car", PopulateCarPanel);
			tabBuilder.AddTab("Equipment", "equipment", PopulateEquipmentPanel);
			if (_car.IsPassengerCar())
			{
				tabBuilder.AddTab("Passenger", "pass", PopulatePassengerCarPanel);
			}
			if (_car.Archetype != CarArchetype.Tender)
			{
				tabBuilder.AddTab("Operations", "ops", PopulateOperationsPanel);
				_observers.Add(_car.KeyValueObject.Observe("ops.waybill", delegate
				{
					Rebuild();
				}, callInitial: false));
			}
		});
	}

	private void PopulateCarPanel(UIPanelBuilder builder)
	{
		UIPanelBuilder.Frequency updateFrequency = UIPanelBuilder.Frequency.Fast;
		builder.AddConditionField(_car);
		builder.AddField("Brake Line", () => Mathf.RoundToInt(_car.air.BrakeLine.Pressure).ToString(), updateFrequency).Tooltip("Brake Line", "Pressure in the brake line is lowered to apply brakes. Brakes recharge when the pressure is raised.");
		builder.HStack(delegate(UIPanelBuilder hstack)
		{
			hstack.AddField("Cylinder", hstack.HStack(delegate(UIPanelBuilder field)
			{
				field.AddLabel(() => Mathf.RoundToInt(_car.air.BrakeCylinder.Pressure).ToString(), updateFrequency).Tooltip("Cylinder", "Reflects how much braking force is being applied.").FlexibleWidth();
				if (_car.SupportsBleed())
				{
					field.AddButtonCompact("Bleed", _car.SetBleed).Tooltip("Bleed Valve", "Bleed the brakes to release pressure from the car's brake system.");
				}
			}));
		});
		builder.HStack(delegate(UIPanelBuilder hstack)
		{
			hstack.AddField("Hand Brake", hstack.HStack(delegate(UIPanelBuilder field)
			{
				field.AddObserver(_car.KeyValueObject.Observe(PropertyChange.KeyForControl(PropertyChange.Control.Handbrake), delegate
				{
					field.Rebuild();
				}, callInitial: false));
				bool handbrakeApplied = _car.air.handbrakeApplied;
				field.AddLabel(handbrakeApplied ? "Applied" : "Released").Tooltip("Hand Brake", "Apply the hand brake to manually apply brakes. Hand brakes should be applied on spotted cars.").FlexibleWidth();
				field.AddButtonCompact(handbrakeApplied ? "Release" : "Apply", ToggleHandBrake).Tooltip("Hand Brake Control", "Apply the hand brake to manually apply brakes. Hand brakes should be applied on spotted cars.");
			}));
		});
		if (_car.IsLocomotive)
		{
			CarControlProperties carControlProperties = _car.ControlProperties;
			builder.AddField("Cut Out", builder.AddToggle(() => carControlProperties[PropertyChange.Control.CutOut], delegate(bool cutOut)
			{
				carControlProperties[PropertyChange.Control.CutOut] = cutOut;
				if (!cutOut)
				{
					carControlProperties[PropertyChange.Control.Mu] = false;
					builder.Rebuild();
				}
			}));
			builder.AddField("MU", builder.AddToggle(() => carControlProperties[PropertyChange.Control.Mu], delegate(bool mu)
			{
				carControlProperties[PropertyChange.Control.Mu] = mu;
				if (mu)
				{
					carControlProperties[PropertyChange.Control.CutOut] = true;
					builder.Rebuild();
				}
			}));
		}
		builder.AddExpandingVerticalSpacer();
		builder.HStack(delegate(UIPanelBuilder hstack)
		{
			hstack.AddButton("Select", SelectConsist).Tooltip("Select Car", "Selected locomotives display HUD controls. Shortcuts allow jumping to the selected car.");
			hstack.AddButton("Follow", delegate
			{
				CameraSelector.shared.FollowCar(_car);
			}).Tooltip("Follow Car", "Jump the overhead camera to this car and track it.");
			hstack.Spacer();
			if (IsSandbox)
			{
				hstack.AddButton("Delete", DeleteConsist).Tooltip("Delete Car", "Click to delete this car. Shift-Click deletes all coupled cars.");
			}
		});
	}

	private void PopulateEquipmentPanel(UIPanelBuilder builder)
	{
		builder.AddConditionField(_car);
		builder.AddMileageField(_car);
		if (_car.Condition < 0.999f)
		{
			string value = GameDateTimeInterval.DeltaStringMinutes((int)(RepairTrack.CalculateRepairWorkOverall(_car) * 60f * 24f));
			builder.AddField("Repair Estimate", value);
		}
		builder.AddRepairDestination(_car);
		builder.Spacer(2f);
		if (EquipmentPurchase.CarCanBeSold(_car))
		{
			builder.AddSellDestination(_car);
			builder.Spacer(2f);
		}
		builder.AddExpandingVerticalSpacer();
		string reason;
		bool flag = CanCustomize(out reason);
		if (flag || !string.IsNullOrEmpty(reason))
		{
			IConfigurableElement configurableElement = builder.AddButton("Customize", ShowCustomize);
			if (!flag)
			{
				configurableElement.Disable(disable: true).Tooltip("Customize Not Available", reason);
			}
		}
	}

	private void SelectConsist()
	{
		TrainController.Shared.SelectedCar = _car;
		if (GameInput.IsShiftDown)
		{
			_window.CloseWindow();
		}
	}

	private void DeleteConsist()
	{
		if (GameInput.IsShiftDown)
		{
			TrainController.Shared.RemoveAllCarsCoupledTo(_car.id);
		}
		else
		{
			TrainController.Shared.RemoveCarSmart(_car.id);
		}
		_window.CloseWindow();
	}

	private void ToggleHandBrake()
	{
		_car.SetHandbrake(!_car.air.handbrakeApplied);
	}

	private static string WindowTitleForCar(Car car)
	{
		return car.DisplayName;
	}

	private static string TitleForCar(Car car)
	{
		return "<b><size=80%>" + car.CarType + "</size></b> " + car.DisplayName;
	}

	private static string SubtitleForCar(Car car)
	{
		int num = Mathf.CeilToInt(car.Weight / 2000f);
		string arg = (string.IsNullOrEmpty(car.DefinitionInfo.Metadata.Name) ? car.CarType : car.DefinitionInfo.Metadata.Name);
		return $"{num}T {arg}";
	}

	private bool CanCustomize(out string reason)
	{
		return UI.CarCustomizeWindow.CarCustomizeWindow.CanCustomize(_car, out reason);
	}

	private void ShowCustomize()
	{
		UI.CarCustomizeWindow.CarCustomizeWindow.Show(_car);
	}

	private void PopulateOperationsPanel(UIPanelBuilder builder)
	{
		builder.VScrollView(delegate(UIPanelBuilder builder2)
		{
			builder2.FieldLabelWidth = 100f;
			if (builder2.AddTrainCrewDropdown(_car))
			{
				TrainCrew trainCrew2;
				TrainCrew trainCrew = (StateManager.Shared.PlayersManager.TrainCrewForId(_car.trainCrewId, out trainCrew2) ? trainCrew2 : null);
				if (trainCrew != null)
				{
					Model.Ops.Timetable.Timetable current = TimetableController.Shared.Current;
					if (current != null)
					{
						builder2.RebuildOnEvent<TimetableDidChange>();
						CrewsPanelBuilder.BuildTimetableSymbolField(builder2, current, trainCrew);
					}
				}
				builder2.Spacer(4f);
			}
			if (_car.Archetype.IsFreight())
			{
				Waybill? waybill = Waybill;
				if (waybill.HasValue)
				{
					PopulateWaybillPanel(builder2, waybill.Value);
				}
				builder2.AddExpandingVerticalSpacer();
				if (CanSetWaybill)
				{
					PopulateSetWaybillPanel(builder2);
				}
			}
		});
	}

	private void PopulateWaybillPanel(UIPanelBuilder builder, Waybill waybill)
	{
		OpsCarPositionDisplayable destinationDisplayable = new OpsCarPositionDisplayable(waybill.Destination);
		builder.AddField("Destination", builder.AddLocationField(waybill.Destination.DisplayName, destinationDisplayable, delegate
		{
			JumpTo(destinationDisplayable);
		}));
		builder.AddField("Status", () => WaybillStatusText(waybill), UIPanelBuilder.Frequency.Periodic).Tooltip(() => WaybillStatusTooltip(waybill));
		if (waybill.Origin.HasValue)
		{
			OpsCarPositionDisplayable originDisplayable = new OpsCarPositionDisplayable(waybill.Origin.Value);
			string text = waybill.Created.IntervalString(TimeWeather.Now);
			builder.AddField("Origin", builder.AddLocationField(waybill.Origin.Value.DisplayName, originDisplayable, delegate
			{
				JumpTo(originDisplayable);
			})).Tooltip("Origin", "Position of this car at the time this waybill was created, " + text + " ago.");
		}
		else
		{
			builder.AddField("Issued", () => waybill.Created.IntervalString(TimeWeather.Now) + " ago", UIPanelBuilder.Frequency.Periodic);
		}
		TrainCrew myTrainCrew = StateManager.Shared.PlayersManager.MyTrainCrew;
		string carId;
		if (myTrainCrew != null)
		{
			carId = _car.id;
			SwitchListPanel switchListPanel = SwitchListPanel.Shared;
			builder.RebuildOnEvent<SwitchListDidChange>();
			builder.AddField("On Switch List", builder.AddToggle(() => switchListPanel.SwitchListContains(carId), SetOnSwitchList));
		}
		void SetOnSwitchList(bool on)
		{
			StateManager.ApplyLocal(new SwitchListToggleCarIds(myTrainCrew.Id, new List<string> { carId }, on));
		}
	}

	private string WaybillStatusText(Waybill waybill)
	{
		GetWaybillStatusInfo(waybill, out var completed, out var basePayment, out var currentBonus, out var nextBonusTime, out var _, out var conditionFine);
		if (completed)
		{
			return "Completed";
		}
		if (basePayment == 0)
		{
			return "Pending Delivery";
		}
		StringBuilder stringBuilder = new StringBuilder();
		int num = basePayment + currentBonus - conditionFine;
		stringBuilder.Append($"${num} on delivery");
		if (currentBonus > 0)
		{
			GameDateTime now = TimeWeather.Now;
			string text = nextBonusTime.IntervalString(now, GameDateTimeInterval.Style.Short);
			stringBuilder.Append(" in next " + text);
		}
		return stringBuilder.ToString();
	}

	private TooltipInfo WaybillStatusTooltip(Waybill waybill)
	{
		GetWaybillStatusInfo(waybill, out var completed, out var basePayment, out var currentBonus, out var nextBonusTime, out var nextBonus, out var conditionFine);
		if (completed)
		{
			return new TooltipInfo("Completed", "This car has been spotted at its destination.");
		}
		if (basePayment == 0)
		{
			return new TooltipInfo("Pending Delivery", "This car has not been spotted at its destination.");
		}
		GameDateTime now = TimeWeather.Now;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"${basePayment} to be paid on delivery");
		if (currentBonus > 0)
		{
			string arg = nextBonusTime.IntervalString(now, GameDateTimeInterval.Style.Short);
			stringBuilder.AppendLine($"+ ${currentBonus} if delivered in next {arg}; ${nextBonus} thereafter");
		}
		if (conditionFine > 0)
		{
			stringBuilder.AppendLine($"- ${conditionFine} for damage");
		}
		return new TooltipInfo("Pending Delivery", stringBuilder.ToString().Trim());
	}

	private void GetWaybillStatusInfo(Waybill waybill, out bool completed, out int basePayment, out int currentBonus, out GameDateTime nextBonusTime, out int nextBonus, out int conditionFine)
	{
		basePayment = 0;
		currentBonus = 0;
		nextBonusTime = default(GameDateTime);
		nextBonus = 0;
		conditionFine = 0;
		completed = waybill.Completed;
		if (completed)
		{
			return;
		}
		basePayment = waybill.PaymentOnArrival;
		if (!OpsController.Shared.TryGetActiveContract(waybill.Destination, out var contract))
		{
			return;
		}
		int num = Mathf.FloorToInt(TimeWeather.Now.DaysSince(waybill.Created));
		currentBonus = contract.TimelyDeliveryBonus(num, waybill.PaymentOnArrival);
		if (currentBonus > 0)
		{
			for (int i = num + 1; i <= 2; i++)
			{
				nextBonus = contract.TimelyDeliveryBonus(Mathf.FloorToInt(i), waybill.PaymentOnArrival);
				if (nextBonus != currentBonus)
				{
					nextBonusTime = waybill.Created.AddingDays(i);
					break;
				}
			}
		}
		conditionFine = waybill.ConditionFineForCarCondition(_car.Condition);
	}

	private void PopulateSetWaybillPanel(UIPanelBuilder builder)
	{
		OpsController opsController = OpsController.Shared;
		builder.AddSection("Automatic Waybills", delegate(UIPanelBuilder builder2)
		{
			builder2.FieldLabelWidth = 100f;
			AddDestinationDropdown("Unload", AutoDestinationType.Load, builder2);
			AddDestinationDropdown("Load", AutoDestinationType.Empty, builder2);
			builder2.AddField("Actions", builder2.ButtonStrip(delegate(UIPanelBuilder uIPanelBuilder)
			{
				uIPanelBuilder.AddButton("<sprite name=Copy><sprite name=Coupled>", delegate
				{
					CopyAutoDestinationToCoupled(opsController);
				}).Tooltip("Copy Auto Waybills to Coupled", "Copy automatic waybill assignments to coupled cars.");
				if (CanCycleWaybill())
				{
					OpsController opsController2 = OpsController.Shared;
					uIPanelBuilder.AddButton("<sprite name=CycleWaybills>", delegate
					{
						opsController2.CycleAutoWaybill(_car);
						uIPanelBuilder.Rebuild();
					}).Tooltip("Cycle Waybill", "Flip this car's waybill to the other automatic waybill destination.");
					uIPanelBuilder.AddButton("<sprite name=CycleWaybills><sprite name=Coupled>", delegate
					{
						opsController2.CycleAutoWaybill(_car, _car.EnumerateCoupled());
						uIPanelBuilder.Rebuild();
					}).Tooltip("Cycle Waybill (Coupled)", "Flip this car's waybill - and those it is coupled to which match - to the other automatic waybill destination.");
				}
			}));
		});
		void AddDestinationDropdown(string title, AutoDestinationType destinationType, UIPanelBuilder uIPanelBuilder)
		{
			List<(IndustryComponent ic, Area area)> options = new List<(IndustryComponent, Area)>();
			AddDropdownItem(null, null);
			foreach (Area area in opsController.Areas)
			{
				foreach (Industry item in area.Industries.Where(ShouldShowIndustry))
				{
					foreach (IndustryComponent item2 in item.VisibleComponents.Where((IndustryComponent ic) => ic.carTypeFilter.Matches(_car.CarType)))
					{
						if (ShouldShow(item2))
						{
							AddDropdownItem(item2, area);
						}
					}
				}
			}
			string title2 = "Automatic Waybills: " + ((destinationType == AutoDestinationType.Empty) ? "Load" : "Unload") + " Location";
			string message = ((destinationType == AutoDestinationType.Empty) ? "Location to load this car. A waybill will be assigned to here once it is empty." : "Location to unload this car. A waybill will be assigned to here once it is loaded.");
			string prompt = destinationType switch
			{
				AutoDestinationType.Empty => "Select a location to load this car. A waybill will be assigned to here once it is empty.", 
				AutoDestinationType.Load => "Select a location to unload this car. A waybill will be assigned to here once it is loaded.", 
				_ => throw new ArgumentOutOfRangeException("destinationType", destinationType, null), 
			};
			OpsCarPosition? currentDestination = _car.GetAutoDestination(destinationType, opsController);
			IndustryComponent selected = ((!currentDestination.HasValue) ? null : options.FirstOrDefault(((IndustryComponent ic, Area area) tuple) => tuple.ic != null && tuple.ic.Identifier == currentDestination.Value.Identifier).ic);
			uIPanelBuilder.AddField(title, uIPanelBuilder.AddLocationPicker(prompt, options, selected, delegate(IndustryComponent newlySelected)
			{
				SetAutoDestination(destinationType, newlySelected, opsController);
				uIPanelBuilder.Rebuild();
			})).Tooltip(title2, message);
			void AddDropdownItem(IndustryComponent ic, Area area)
			{
				options.Add((ic, area));
			}
			bool ShouldShow(IndustryComponent ic)
			{
				if (ic is Interchange)
				{
					return false;
				}
				if (ic is RepairTrack)
				{
					return false;
				}
				return ic.WantsAutoDestination(destinationType);
			}
		}
		bool CanCycleWaybill()
		{
			Waybill? waybill = Waybill;
			if (!waybill.HasValue)
			{
				return true;
			}
			if (!StateManager.HasTrainmasterAccess)
			{
				return false;
			}
			if (IsSandbox)
			{
				return true;
			}
			return waybill.Value.Tag == "autodest";
		}
		static bool ShouldShowIndustry(Industry industry)
		{
			if (industry.ProgressionDisabled)
			{
				return false;
			}
			if (industry.usesContract)
			{
				return industry.HasActiveContract(TimeWeather.Now);
			}
			return true;
		}
	}

	private void CopyAutoDestinationToCoupled(IOpsCarPositionResolver opsController)
	{
		OpsCarPosition? autoDestination = _car.GetAutoDestination(AutoDestinationType.Load, opsController);
		OpsCarPosition? autoDestination2 = _car.GetAutoDestination(AutoDestinationType.Empty, opsController);
		int num = 0;
		foreach (Car item in _car.EnumerateCoupled())
		{
			if (item.Archetype.IsFreight() && item.IsOwnedByPlayer)
			{
				bool flag = false;
				if (item.SetAutoDestination(AutoDestinationType.Load, autoDestination))
				{
					flag = true;
				}
				if (item.SetAutoDestination(AutoDestinationType.Empty, autoDestination2))
				{
					flag = true;
				}
				if (flag)
				{
					item.ApplyAutoWaybillIfNeeded(opsController);
					num++;
				}
			}
		}
		ToastPresentCopied(num);
	}

	private void SetAutoDestination(AutoDestinationType destinationType, [CanBeNull] IndustryComponent ic, OpsController opsController)
	{
		_car.SetAutoDestination(destinationType, (ic == null) ? ((OpsCarPosition?)null) : new OpsCarPosition?(ic));
		_car.ApplyAutoWaybillIfNeeded(opsController);
	}

	private void PopulatePassengerCarPanel(UIPanelBuilder builder)
	{
		builder.AddField("Passengers", () => _car.PassengerCountString(_cachedMarker), UIPanelBuilder.Frequency.Fast);
		if (_stopsLookup == null || _stopsLookup.Count == 0)
		{
			_stopsLookup = PassengerStop.FindAll().ToDictionary((PassengerStop stop) => stop.identifier, (PassengerStop stop) => stop);
		}
		List<PassengerStop> orderedStops = (from id in new string[15]
			{
				"sylva", "dillsboro", "wilmot", "whittier", "ela", "bryson", "hemingway", "alarkajct", "almond", "nantahala",
				"topton", "rhodo", "andrews", "cochran", "alarka"
			}
			select _stopsLookup[id] into ps
			where !ps.ProgressionDisabled
			select ps).ToList();
		builder.VScrollView(delegate(UIPanelBuilder uIPanelBuilder)
		{
			foreach (PassengerStop item in orderedStops)
			{
				PassengerStop stop = item;
				uIPanelBuilder.HStack(delegate(UIPanelBuilder hstack)
				{
					hstack.AddToggle(() => IsPassengerStopChecked(stop), delegate(bool isOn)
					{
						SetPassengerStopChecked(stop, isOn);
					});
					hstack.AddLocationField(FieldName, stop, delegate
					{
						JumpTo(stop);
					});
				});
				string FieldName()
				{
					if (!_cachedMarker.HasValue)
					{
						return stop.name;
					}
					int num = _cachedMarker.Value.CountPassengersForStop(stop.identifier);
					if (num != 0)
					{
						return stop.name + " (" + num + ")";
					}
					return stop.name;
				}
			}
		});
		builder.Spacer(6f);
		builder.ButtonStrip(delegate(UIPanelBuilder uIPanelBuilder)
		{
			uIPanelBuilder.AddButtonCompact("<sprite name=Copy><sprite name=Coupled>", CopyStopsToCoupledCoaches).Tooltip("Copy to Coupled", "Copy passenger stops to coupled coaches.");
			if (_car.TryGetTimetableTrain(out var train))
			{
				uIPanelBuilder.AddButtonCompact("Copy from Timetable", delegate
				{
					_car.CopyStopsFromTimetable();
				}).Tooltip("Set from Timetable", "Copy the passenger stops from the timetable for " + train.Name + ".");
				uIPanelBuilder.AddToggle(() => _cachedMarker?.AutoDestinationsFromTimetable ?? false, delegate(bool en)
				{
					StateManager.ApplyLocal(new SetPassengerAutoDestinations(_car.id, en));
					if (en)
					{
						_car.CopyStopsFromTimetable();
					}
				});
				uIPanelBuilder.AddLabel("Auto Dest").Tooltip("Timetable Auto Destinations", "Automatically sets destinations from this train's timetable schedule, including for passenger transfers, while car is stopped at a station.");
			}
		});
		_observers.Add(_car.KeyValueObject.Observe("ops.passengerMarker", delegate
		{
			if (!(_car == null))
			{
				_cachedMarker = _car.GetPassengerMarker();
			}
		}));
	}

	private void SetPassengerStopChecked(PassengerStop passengerStop, bool isOn)
	{
		PassengerMarker passengerMarkerOrEmpty = GetPassengerMarkerOrEmpty();
		string identifier = passengerStop.identifier;
		HashSet<string> destinations = passengerMarkerOrEmpty.Destinations;
		bool flag = destinations.Contains(identifier);
		if (isOn && !flag)
		{
			destinations.Add(identifier);
		}
		else if (!isOn && flag)
		{
			destinations.Remove(identifier);
		}
		StateManager.ApplyLocal(new SetPassengerDestinations(_car.id, destinations.ToList()));
	}

	private PassengerMarker GetPassengerMarkerOrEmpty()
	{
		return _car.GetPassengerMarker() ?? PassengerMarker.Empty();
	}

	private bool IsPassengerStopChecked(PassengerStop passengerStop)
	{
		if (_cachedMarker.HasValue)
		{
			return _cachedMarker.Value.Destinations.Contains(passengerStop.identifier);
		}
		return false;
	}

	private void CopyStopsToCoupledCoaches()
	{
		PassengerMarker passengerMarkerOrEmpty = GetPassengerMarkerOrEmpty();
		HashSet<string> destinations = passengerMarkerOrEmpty.Destinations;
		int num = 0;
		foreach (Car item in _car.EnumerateCoupled())
		{
			if (item.IsPassengerCar())
			{
				StateManager.ApplyLocal(new SetPassengerDestinations(item.id, destinations.ToList()));
				StateManager.ApplyLocal(new SetPassengerAutoDestinations(item.id, passengerMarkerOrEmpty.AutoDestinationsFromTimetable));
				num++;
			}
		}
		ToastPresentCopied(num);
	}

	private void JumpTo(IIndustryTrackDisplayable passengerStop)
	{
		Log.Debug("Jump to {industry}", passengerStop);
		if (!passengerStop.TrackSpans.Any())
		{
			Log.Error("Industry has no track spans? {industry}", passengerStop);
		}
		else
		{
			CameraSelector.shared.ZoomToPoint(passengerStop.CenterPoint);
		}
	}

	private static void ToastPresentCopied(int count)
	{
		Toast.Present("Copied to " + (count - 1).Pluralize("other"));
	}
}
