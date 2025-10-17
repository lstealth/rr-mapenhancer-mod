using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core;
using Game.Messages;
using Game.State;
using Model;
using Model.Ops;
using Serilog;
using TMPro;
using UI.Common;
using UnityEngine;

namespace UI.SwitchList;

[RequireComponent(typeof(Window))]
public class SwitchListPanel : MonoBehaviour
{
	private static SwitchListPanel _panel;

	private Window _window;

	public SwitchListRow rowTemplate;

	public RectTransform scrollContent;

	[SerializeField]
	private TMP_Text emptyStateLabel;

	[SerializeField]
	private DropdownMenu toolsMenu;

	private Coroutine _refreshCoroutine;

	private readonly OpsCarList _switchList = new OpsCarList();

	private TrainController trainController => TrainController.Shared;

	private static TrainCrew TrainCrew => StateManager.Shared.PlayersManager.MyTrainCrew;

	public static SwitchListPanel Shared => WindowManager.Shared.GetWindow<SwitchListPanel>();

	public static void Show()
	{
		Shared._Show();
	}

	private void _Show()
	{
		_window.Title = "Switch List";
		_window.ShowWindow();
	}

	public static void Toggle()
	{
		if (Shared._window.IsShown)
		{
			Shared._window.CloseWindow();
		}
		else
		{
			Shared._Show();
		}
	}

	private void Start()
	{
		_panel = this;
		_window = GetComponent<Window>();
		rowTemplate.gameObject.SetActive(value: false);
		SetEmptyStateText(null);
		toolsMenu.Configure(new DropdownMenu.RowData[4]
		{
			new DropdownMenu.RowData("Add from Train", "Add cars in selected train to the switch list"),
			new DropdownMenu.RowData("Cleanup", "Remove completed rows"),
			new DropdownMenu.RowData("Sort by Destination", null),
			new DropdownMenu.RowData("Sort by Location", null)
		}, delegate(int index)
		{
			switch (index)
			{
			case 0:
				ClickAddFromTrain();
				break;
			case 1:
				ClickCleanup();
				break;
			case 2:
				ClickSortByDestination();
				break;
			case 3:
				ClickSortByCurrentLocation();
				break;
			default:
				throw new ArgumentOutOfRangeException("index", index, null);
			}
		});
	}

	private void OnEnable()
	{
		_refreshCoroutine = StartCoroutine(PeriodicRefresh());
	}

	private void OnDisable()
	{
		if (_refreshCoroutine != null)
		{
			StopCoroutine(_refreshCoroutine);
		}
		_refreshCoroutine = null;
	}

	private void ClickSortByDestination()
	{
		IEnumerable<string> carIds = from entry in _switchList.Entries
			orderby entry.Destination.SortOrder, entry.Current.SortOrder, entry.CarSortName
			select entry.CarId;
		ApplySwitchListSetCarIds(carIds);
	}

	private void ClickSortByCurrentLocation()
	{
		IEnumerable<string> carIds = from entry in _switchList.Entries
			orderby entry.Current.SortOrder, entry.CarSortName
			select entry.CarId;
		ApplySwitchListSetCarIds(carIds);
	}

	private void ClickCleanup()
	{
		IEnumerable<string> carIds = from e in _switchList.Entries
			where !e.Completed
			select e.CarId;
		ApplySwitchListSetCarIds(carIds);
	}

	private void ClickAddFromTrain()
	{
		List<Car> list = TrainController.Shared.SelectedTrain.ToList();
		if (list.Count == 0)
		{
			Toast.Present("No selected cars.");
			return;
		}
		TrainCrew myTrainCrew = StateManager.Shared.PlayersManager.MyTrainCrew;
		if (myTrainCrew == null)
		{
			Toast.Present("Join or create a train crew first.");
			return;
		}
		List<string> list2 = (from car in list.Where(delegate(Car car)
			{
				Waybill? waybill = car.GetWaybill(OpsController.Shared);
				if (!waybill.HasValue || waybill.Value.Completed)
				{
					return false;
				}
				return !_switchList.Entries.Any((OpsCarList.Entry e) => e.CarId == car.id);
			})
			select car.id).ToList();
		if (list2.Count == 0)
		{
			Toast.Present("No cars to add.");
			return;
		}
		StateManager.ApplyLocal(new SwitchListToggleCarIds(myTrainCrew.Id, list2, on: true));
		Toast.Present("Add " + list2.Count.Pluralize("car") + " to switch list");
	}

	public static void Refresh(Game.Messages.SwitchList switchList)
	{
		_panel.Rebuild(switchList);
	}

	private void Rebuild(Game.Messages.SwitchList switchList)
	{
		if (!(trainController == null))
		{
			_switchList.Rebuild(switchList.Entries.Select((Game.Messages.SwitchList.Entry e) => e.CarId));
			Log.Debug("Rebuild Switch List: Received {count} entries", _switchList.Entries.Count);
			Rebuild();
		}
	}

	private void Rebuild()
	{
		scrollContent.DestroyChildrenExcept(rowTemplate);
		foreach (OpsCarList.Entry entry in _switchList.Entries)
		{
			try
			{
				InstantiateCell(entry);
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Failed to create switch list cell for {carId}", entry.CarId);
			}
		}
		int count = _switchList.Entries.Count;
		int num = Mathf.Max(Mathf.Min(800, count * 30 + 30), 100);
		_window.SetContentSize(new Vector2(_window.InitialContentSize.x, num));
	}

	private void InstantiateCell(OpsCarList.Entry entry)
	{
		Car car = trainController.CarForId(entry.CarId);
		if (car == null)
		{
			Log.Warning("Couldn't find car for {carId}", entry.CarId);
			return;
		}
		OpsCarList.Entry.Location current = entry.Current;
		OpsCarList.Entry.Location destination = entry.Destination;
		bool completed = entry.Completed;
		_ = car.BodyTransform;
		SwitchListRow switchListRow = UnityEngine.Object.Instantiate(rowTemplate, scrollContent);
		switchListRow.gameObject.SetActive(value: true);
		switchListRow.carName.text = car.DisplayName;
		switchListRow.carType.text = car.CarType;
		switchListRow.destination.text = destination.Title;
		switchListRow.location.text = current.Title;
		string text = car.CarType + " " + car.DisplayName;
		string subtitle = $"{car.Weight:N0} lbs";
		string title = text;
		string subtitle2 = current.Title + " / " + current.Subtitle;
		LocationIndicatorHoverArea component = switchListRow.carName.GetComponent<LocationIndicatorHoverArea>();
		LocationIndicatorHoverArea component2 = switchListRow.destination.GetComponent<LocationIndicatorHoverArea>();
		LocationIndicatorHoverArea component3 = switchListRow.location.GetComponent<LocationIndicatorHoverArea>();
		component.descriptors.Add(new LocationIndicatorController.Descriptor(car.id, text, subtitle));
		component3.descriptors.Add(new LocationIndicatorController.Descriptor(car.id, title, subtitle2));
		component2.descriptors.Add(new LocationIndicatorController.Descriptor(destination.Position, destination.Title, destination.Subtitle));
		component2.spanIds.AddRange(destination.SpanIds);
		component.OnClick += delegate
		{
			CameraSelector.shared.ZoomToCar(car);
		};
		component3.OnClick += delegate
		{
			CameraSelector.shared.ZoomToCar(car);
		};
		component2.OnClick += delegate
		{
			CameraSelector.shared.ZoomToPoint(destination.Position);
		};
		switchListRow.strikethrough.gameObject.SetActive(completed);
		Color color = Color.white * (completed ? 0.65f : 0.85f);
		switchListRow.carType.color = color;
		switchListRow.carName.color = color;
		switchListRow.destination.color = color;
		switchListRow.location.color = color;
		switchListRow.OnRemoveClicked = delegate
		{
			RemoveCarFromList(entry.CarId);
		};
	}

	private IEnumerator PeriodicRefresh()
	{
		WaitForSeconds wait = new WaitForSeconds(2f);
		while (true)
		{
			if (_window != null && _window.IsShown)
			{
				UpdatePositions();
			}
			yield return wait;
		}
	}

	private void UpdatePositions()
	{
		TrainCrew trainCrew = TrainCrew;
		if (trainCrew == null)
		{
			SetEmptyStateText("Join or create a train crew to add cars.");
			return;
		}
		string text = trainCrew.Name;
		_window.Title = (string.IsNullOrEmpty(text) ? "Switch List" : ("Switch List - " + text));
		if (_switchList.Rebuild())
		{
			Log.Debug("Rebuild Switch List: Entries changed");
			Rebuild();
		}
		SetEmptyStateText((_switchList.Entries.Count == 0) ? "Switch list is empty!\nAdd cars at station agent window." : null);
	}

	private void SetEmptyStateText(string text)
	{
		emptyStateLabel.text = text;
		emptyStateLabel.gameObject.SetActive(!string.IsNullOrEmpty(text));
	}

	private void RemoveCarFromList(string carId)
	{
		IEnumerable<string> carIds = from e in _switchList.Entries
			where e.CarId != carId
			select e.CarId;
		ApplySwitchListSetCarIds(carIds);
	}

	private void ApplySwitchListSetCarIds(IEnumerable<string> carIds)
	{
		if (string.IsNullOrEmpty(TrainCrew.Id))
		{
			Log.Error("ApplySwitchListSetCarIds: no train crew id");
		}
		else
		{
			StateManager.ApplyLocal(new SwitchListSetCarIds(TrainCrew.Id, carIds.ToList()));
		}
	}

	private void InstantiateLabelCell(string text)
	{
		SwitchListRow switchListRow = UnityEngine.Object.Instantiate(rowTemplate, scrollContent);
		switchListRow.gameObject.SetActive(value: true);
		switchListRow.carType.text = text;
		switchListRow.carName.text = "";
		switchListRow.destination.text = "";
		switchListRow.location.text = "";
		switchListRow.strikethrough.gameObject.SetActive(value: false);
	}

	public bool SwitchListContains(string carId)
	{
		return _switchList.Entries.Any((OpsCarList.Entry e) => e.CarId == carId);
	}
}
