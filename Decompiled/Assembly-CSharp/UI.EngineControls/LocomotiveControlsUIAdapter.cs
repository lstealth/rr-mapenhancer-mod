using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using Game;
using Game.Events;
using Game.Messages;
using Game.State;
using Model;
using Model.AI;
using Model.Ops.Timetable;
using TMPro;
using UI.CarInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace UI.EngineControls;

public class LocomotiveControlsUIAdapter : MonoBehaviour
{
	[SerializeField]
	private TMP_Text nameLabel;

	[SerializeField]
	private TMP_Text infoALabel;

	[SerializeField]
	private TMP_Text infoBLabel;

	[SerializeField]
	private TMP_Text speedLabel;

	[FormerlySerializedAs("dropdown")]
	[Tooltip("Mode selection dropdown: Manual, AI Road, AI Yard.")]
	[SerializeField]
	private TMP_Dropdown modeDropdown;

	[SerializeField]
	private DropdownMenu optionsDropdown;

	[SerializeField]
	private RectTransform controls;

	[SerializeField]
	private EngineControlSetBase manualControls;

	[SerializeField]
	private EngineControlSetBase simplifiedControls;

	[SerializeField]
	private EngineControlSetBase aiRoadControls;

	[SerializeField]
	private EngineControlSetBase aiYardControls;

	[SerializeField]
	private EngineControlSetBase aiWaypointControls;

	private Coroutine _coroutine;

	private Coroutine _speedometerCoroutine;

	private List<EngineControlSetBase> _controlSets;

	private AutoEngineerPersistence _persistence;

	private IDisposable _ordersObserver;

	private static BaseLocomotive Locomotive => TrainController.Shared?.SelectedLocomotive;

	private bool SimplifiedControls
	{
		get
		{
			return Preferences.SimplifiedControls;
		}
		set
		{
			Preferences.SimplifiedControls = value;
		}
	}

	private void OnEnable()
	{
		_coroutine = StartCoroutine(UpdateLocomotiveTextCoroutine());
		_controlSets = new List<EngineControlSetBase> { manualControls, simplifiedControls, aiRoadControls, aiYardControls, aiWaypointControls };
		modeDropdown.ClearOptions();
		modeDropdown.AddOptions(new List<string> { "Manual", "AE Road", "AE Yard", "AE Waypoint" });
		UpdateForSelectedCar();
		Messenger.Default.Register<SelectedCarChanged>(this, delegate
		{
			UpdateForSelectedCar();
		});
		Messenger.Default.Register<CarIdentChanged>(this, delegate
		{
			UpdateForSelectedCar();
		});
	}

	private void OnDisable()
	{
		_ordersObserver?.Dispose();
		_ordersObserver = null;
		StopCoroutine(_coroutine);
		if (_speedometerCoroutine != null)
		{
			StopCoroutine(_speedometerCoroutine);
			_speedometerCoroutine = null;
		}
		Messenger.Default.Unregister(this);
	}

	private void UpdateOptionsDropdown(EngineControlSetBase selected)
	{
		OptionsDropdownConfiguration optionsDropdownConfiguration = selected.ConfigureOptionsDropdown();
		List<DropdownMenu.RowData> list = optionsDropdownConfiguration.Rows;
		Action<int> action = optionsDropdownConfiguration.OnRowSelected;
		if (_persistence.Orders.Mode == AutoEngineerMode.Off)
		{
			list = list.ToList();
			list.Insert(0, new DropdownMenu.RowData(SimplifiedControls ? DropdownMenu.CheckState.Checked : DropdownMenu.CheckState.Unchecked, "Simplified Controls", null));
			Action<int> suppliedOnRowSelected = action;
			action = delegate(int row)
			{
				if (row == 0)
				{
					SimplifiedControls = !SimplifiedControls;
					UpdateSelectedControlSet();
				}
				else
				{
					suppliedOnRowSelected(row - 1);
				}
			};
		}
		optionsDropdown.Configure(list, action);
		optionsDropdown.interactable = list.Count > 0;
	}

	private void UpdateForSelectedCar()
	{
		BaseLocomotive locomotive = Locomotive;
		controls.gameObject.SetActive(locomotive != null);
		if (locomotive == null)
		{
			if (_speedometerCoroutine != null)
			{
				StopCoroutine(_speedometerCoroutine);
				_speedometerCoroutine = null;
			}
			return;
		}
		nameLabel.text = locomotive.DisplayName;
		_persistence = new AutoEngineerPersistence(locomotive.KeyValueObject);
		_ordersObserver?.Dispose();
		_ordersObserver = _persistence.ObserveOrders(OrdersDidChange);
		UpdateCarText();
		if (_speedometerCoroutine == null)
		{
			_speedometerCoroutine = StartCoroutine(UpdateSpeedCoroutine());
		}
	}

	private void OrdersDidChange(Orders orders)
	{
		UpdateSelectedControlSet(orders);
	}

	private void UpdateSelectedControlSet()
	{
		UpdateSelectedControlSet(_persistence.Orders);
	}

	private void UpdateSelectedControlSet(Orders orders)
	{
		AutoEngineerMode mode = orders.Mode;
		EngineControlSetBase engineControlSetBase = SelectedControlSet(mode);
		foreach (EngineControlSetBase controlSet in _controlSets)
		{
			controlSet.gameObject.SetActive(controlSet == engineControlSetBase);
		}
		engineControlSetBase.OnOrdersDidChange(orders);
		TMP_Dropdown tMP_Dropdown = modeDropdown;
		tMP_Dropdown.SetValueWithoutNotify(mode switch
		{
			AutoEngineerMode.Off => 0, 
			AutoEngineerMode.Road => 1, 
			AutoEngineerMode.Yard => 2, 
			AutoEngineerMode.Waypoint => 3, 
			_ => throw new ArgumentOutOfRangeException(), 
		});
		UpdateOptionsDropdown(engineControlSetBase);
	}

	private EngineControlSetBase SelectedControlSet(AutoEngineerMode mode)
	{
		return mode switch
		{
			AutoEngineerMode.Off => SimplifiedControls ? simplifiedControls : manualControls, 
			AutoEngineerMode.Road => aiRoadControls, 
			AutoEngineerMode.Yard => aiYardControls, 
			AutoEngineerMode.Waypoint => aiWaypointControls, 
			_ => throw new ArgumentOutOfRangeException("mode", mode, null), 
		};
	}

	public void DidClickInspect()
	{
		UI.CarInspector.CarInspector.Show(Locomotive);
	}

	public void DidClickFollow()
	{
		CameraSelector.shared.FollowCar(Locomotive);
	}

	public void DropdownDidChange(int value)
	{
		AutoEngineerMode autoEngineerMode = value switch
		{
			0 => AutoEngineerMode.Off, 
			1 => AutoEngineerMode.Road, 
			2 => AutoEngineerMode.Yard, 
			3 => AutoEngineerMode.Waypoint, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
		AutoEngineerOrdersHelper autoEngineerOrdersHelper = new AutoEngineerOrdersHelper(Locomotive, _persistence);
		if (autoEngineerOrdersHelper.Mode != autoEngineerMode)
		{
			int? num = ((autoEngineerMode == AutoEngineerMode.Yard) ? new int?(0) : ((int?)null));
			AutoEngineerMode? mode = autoEngineerMode;
			int? maxSpeedMph = num;
			autoEngineerOrdersHelper.SetOrdersValue(mode, null, maxSpeedMph);
		}
	}

	private IEnumerator UpdateLocomotiveTextCoroutine()
	{
		while (true)
		{
			UpdateCarText();
			yield return new WaitForSecondsRealtime(1f);
		}
	}

	private IEnumerator UpdateSpeedCoroutine()
	{
		WaitForSecondsRealtime wait = new WaitForSecondsRealtime(0.1f);
		while (true)
		{
			float velocityMphAbs = Locomotive.VelocityMphAbs;
			int num = ((velocityMphAbs >= 1f) ? Mathf.RoundToInt(velocityMphAbs) : ((velocityMphAbs > 0.1f) ? 1 : 0));
			int num2 = num;
			speedLabel.SetText("<mspace=0.55em>{0}</mspace>\n<color=#5D5B55><size=20%>MPH</size></color>", num2);
			yield return wait;
		}
	}

	private void UpdateCarText()
	{
		BaseLocomotive locomotive = Locomotive;
		if (locomotive == null)
		{
			return;
		}
		string text = LocomotiveControlsHoverArea.SummaryText();
		infoALabel.text = text;
		string text2 = null;
		if (StateManager.Shared.PlayersManager.TrainCrewForId(locomotive.trainCrewId, out var trainCrew))
		{
			text2 = trainCrew.Name;
			if (TimetableController.Shared.TryGetTrainForTrainCrew(trainCrew, out var timetableTrain))
			{
				text2 = text2 + " (Train " + timetableTrain.DisplayStringShort + ")";
			}
		}
		infoBLabel.text = text2;
	}
}
