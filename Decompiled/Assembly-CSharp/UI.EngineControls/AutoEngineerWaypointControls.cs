using System;
using System.Collections.Generic;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.Messages;
using Game.State;
using Model;
using Model.AI;
using TMPro;
using Track;
using UI.Common;
using UI.Tooltips;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI.EngineControls;

public class AutoEngineerWaypointControls : AutoEngineerControlSetBase
{
	private static AutoEngineerWaypointControls _shared;

	[SerializeField]
	private CarControlSlider maxSpeedSlider;

	[SerializeField]
	private TMP_Text maxSpeedLabel;

	[SerializeField]
	private Button setWaypointButton;

	[FormerlySerializedAs("stopButton")]
	[SerializeField]
	private Button clearWaypointButton;

	[SerializeField]
	private Image setWaypointButtonImage;

	[FormerlySerializedAs("buttonNormalSprite")]
	[SerializeField]
	private Sprite spriteChooseWaypoint;

	[FormerlySerializedAs("buttonActiveSprite")]
	[SerializeField]
	private Sprite spriteWaypointActive;

	[SerializeField]
	private Sprite spriteReroute;

	private OrderWaypoint? _lastWaypoint;

	private UITooltipProvider _setWaypointTooltipProvider;

	public static AutoEngineerWaypointControls Shared
	{
		get
		{
			if (_shared == null)
			{
				_shared = UnityEngine.Object.FindObjectOfType<AutoEngineerWaypointControls>();
			}
			return _shared;
		}
	}

	private static AutoEngineerWaypointOverlayController OverlayController => AutoEngineerWaypointOverlayController.Shared;

	private bool ClickSetReroutes
	{
		get
		{
			if (GameInput.IsShiftDown)
			{
				return _lastWaypoint.HasValue;
			}
			return false;
		}
	}

	private void Awake()
	{
		maxSpeedSlider.wholeNumbers = true;
		maxSpeedSlider.minValue = 0f;
		maxSpeedSlider.maxValue = SpeedMphToSlider(AutoEngineerMode.Waypoint.MaxSpeedMph());
		_setWaypointTooltipProvider = setWaypointButton.GetComponent<UITooltipProvider>();
		_setWaypointTooltipProvider.DynamicTooltipInfo = () => (!ClickSetReroutes) ? new TooltipInfo("Choose Waypoint", "Click to Choose Destination Waypoint") : new TooltipInfo("Reroute", "Click to Reroute Engine");
	}

	private void OnEnable()
	{
		Messenger.Default.Register<SelectedCarChanged>(this, delegate
		{
			CancelPicker();
		});
	}

	private void OnDisable()
	{
		Messenger.Default.Unregister(this);
		_lastWaypoint = null;
		if (OverlayController != null)
		{
			OverlayController.Clear();
		}
		CancelPicker();
	}

	private static void CancelPicker()
	{
		if (AutoEngineerDestinationPicker.Shared != null)
		{
			AutoEngineerDestinationPicker.Shared.Cancel();
		}
	}

	private static float SpeedMphToSlider(float mph)
	{
		return Mathf.Round(mph / 5f);
	}

	private static float SliderToSpeedMph(float value)
	{
		return value * 5f;
	}

	protected override void UpdateControls()
	{
		Orders orders = base.OrdersHelper.Orders;
		maxSpeedLabel.SetText("{0}", orders.MaxSpeedMph);
		setWaypointButtonImage.sprite = ((!_lastWaypoint.HasValue) ? spriteChooseWaypoint : (ClickSetReroutes ? spriteReroute : spriteWaypointActive));
	}

	public override void OnOrdersDidChange(Orders orders)
	{
		base.OnOrdersDidChange(orders);
		maxSpeedSlider.SetValueWithoutNotify(SpeedMphToSlider(orders.MaxSpeedMph));
		clearWaypointButton.interactable = orders.Waypoint.HasValue;
		if (!Nullable.Equals(_lastWaypoint, orders.Waypoint))
		{
			_lastWaypoint = orders.Waypoint;
			OverlayController.WaypointDidChange(orders.Waypoint);
		}
	}

	public void WaypointRouteDidUpdate(string locomotiveId, Location? currentStepLocation, bool routeChanged)
	{
		if (!(base.Locomotive.id != locomotiveId))
		{
			OverlayController.WaypointRouteDidUpdate(currentStepLocation, routeChanged);
		}
	}

	public override OptionsDropdownConfiguration ConfigureOptionsDropdown()
	{
		return new OptionsDropdownConfiguration(new List<DropdownMenu.RowData>
		{
			new DropdownMenu.RowData("Jump to Waypoint", null)
		}, delegate(int row)
		{
			if (row == 0)
			{
				JumpToWaypoint();
			}
		});
	}

	private void JumpToWaypoint()
	{
		OrderWaypoint? waypoint = base.OrdersHelper.Orders.Waypoint;
		if (waypoint.HasValue)
		{
			OrderWaypoint valueOrDefault = waypoint.GetValueOrDefault();
			Graph shared = Graph.Shared;
			Location loc = shared.ResolveLocationString(valueOrDefault.LocationString);
			CameraSelector.shared.ZoomToPoint(shared.GetPosition(loc));
		}
		else
		{
			Toast.Present("No waypoint assigned");
		}
	}

	public void HandleMaxSpeedDidChange(float value)
	{
		AutoEngineerOrdersHelper ordersHelper = base.OrdersHelper;
		int? maxSpeedMph = Mathf.RoundToInt(SliderToSpeedMph(value));
		ordersHelper.SetOrdersValue(null, null, maxSpeedMph);
	}

	public void DidClickGoto()
	{
		if (ClickSetReroutes)
		{
			StateManager.ApplyLocal(new AutoEngineerWaypointRerouteRequest(base.Locomotive.id));
		}
		else
		{
			PresentWaypointPicker(base.Locomotive);
		}
	}

	public void DidClickStop()
	{
		base.OrdersHelper.ClearWaypoint();
	}

	public void PresentWaypointPicker(BaseLocomotive locomotive)
	{
		if (!(locomotive != base.Locomotive) && base.OrdersHelper.Mode == AutoEngineerMode.Waypoint)
		{
			AutoEngineerDestinationPicker.Shared.StartPickingLocation(base.Locomotive, base.OrdersHelper);
		}
	}
}
