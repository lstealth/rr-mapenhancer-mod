using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Helpers;
using Model;
using Model.Definition.Data;
using Model.Ops;
using Model.Ops.Definition;
using Serilog;
using UI;
using UI.CarInspector;
using UI.ContextMenu;
using UnityEngine;

namespace RollingStock;

public class CarPickable : MonoBehaviour, IPickable
{
	public Car car;

	private string _cachedTooltipText;

	private float _cachedTooltipTextTime;

	public float MaxPickDistance => 500f;

	public int Priority => -1;

	public PickableActivationFilter ActivationFilter => PickableActivationFilter.Any;

	public TooltipInfo TooltipInfo
	{
		get
		{
			string title = TooltipTitle();
			string text = TooltipText();
			return new TooltipInfo
			{
				Title = title,
				Text = text
			};
		}
	}

	private void Start()
	{
		base.gameObject.layer = Layers.Clickable;
	}

	public void Activate(PickableActivateEvent evt)
	{
		switch (evt.Activation)
		{
		case PickableActivation.Primary:
			if (evt.IsControlDown)
			{
				if (evt.IsShiftDown)
				{
					TrainController.Shared.SelectedCar = car;
				}
				else
				{
					HandleShowInspector(car);
				}
			}
			break;
		case PickableActivation.Secondary:
			HandleShowContextMenu(car);
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	public void Deactivate()
	{
	}

	public static void HandleShowInspector(Car car)
	{
		Log.Information("Activate {car}", car);
		CarInspector.Show(car);
	}

	private static void HandleShowContextMenu(Car car)
	{
		TrainController trainController = TrainController.Shared;
		UI.ContextMenu.ContextMenu shared = UI.ContextMenu.ContextMenu.Shared;
		if (UI.ContextMenu.ContextMenu.IsShown)
		{
			shared.Hide();
		}
		shared.Clear();
		shared.AddButton(ContextMenuQuadrant.General, "Inspect", SpriteName.Inspect, delegate
		{
			HandleShowInspector(car);
		});
		shared.AddButton(ContextMenuQuadrant.General, (trainController.SelectedCar == car) ? "Deselect" : "Select", SpriteName.Select, delegate
		{
			trainController.SelectedCar = ((trainController.SelectedCar == car) ? null : car);
		});
		if (car.SupportsBleed())
		{
			shared.AddButton(ContextMenuQuadrant.Brakes, "Bleed", SpriteName.Bleed, car.SetBleed);
		}
		shared.AddButton(ContextMenuQuadrant.Brakes, car.air.handbrakeApplied ? "Release Handbrake" : "Apply Handbrake", SpriteName.Handbrake, delegate
		{
			bool apply = !car.air.handbrakeApplied;
			car.SetHandbrake(apply);
		});
		shared.Show(car.DisplayName);
	}

	private string TooltipTitle()
	{
		return car.DisplayName;
	}

	private string TooltipText()
	{
		float unscaledTime = Time.unscaledTime;
		if (_cachedTooltipText != null && _cachedTooltipTextTime + 1f > unscaledTime)
		{
			return _cachedTooltipText;
		}
		List<string> list = new List<string>();
		OpsController shared = OpsController.Shared;
		if (shared != null && shared.TryGetDestinationInfo(car, out var destinationName, out var isAtDestination, out var _, out var _))
		{
			string item = (isAtDestination ? "<sprite name=\"Spotted\">" : "<sprite name=\"Destination\">") + " " + destinationName;
			list.Add(item);
		}
		if (car.TryGetTrainName(out var trainName))
		{
			list.Add(trainName);
		}
		bool flag = false;
		bool flag2 = false;
		if (car.Definition.LoadSlots.Count > 0)
		{
			flag2 = true;
			StringBuilder stringBuilder = new StringBuilder();
			foreach (var item5 in car.Definition.DisplayOrderLoadSlots())
			{
				LoadSlot item2 = item5.slot;
				int item3 = item5.index;
				CarLoadInfo? loadInfo = car.GetLoadInfo(item3);
				if (loadInfo.HasValue)
				{
					CarLoadInfo value = loadInfo.Value;
					Load load = CarPrototypeLibrary.instance.LoadForId(value.LoadId);
					if (load == null)
					{
						Debug.LogWarning("Load unknown to library: " + value.LoadId);
						continue;
					}
					stringBuilder.Append(TextSprites.PiePercent(value.Quantity, item2.MaximumCapacity));
					stringBuilder.Append(" ");
					stringBuilder.Append(value.LoadString(load));
					list.Add(stringBuilder.ToString());
					stringBuilder.Clear();
					flag = true;
				}
			}
		}
		if (car.IsPassengerCar())
		{
			flag2 = true;
			PassengerMarker? passengerMarker = car.GetPassengerMarker();
			if (passengerMarker.HasValue)
			{
				string item4 = PassengerString(car, passengerMarker.Value);
				list.Add(item4);
				flag = true;
			}
		}
		if (flag2 && !flag)
		{
			list.Add(TextSprites.PiePercent(0f, 1f) + " Empty");
		}
		if (car.air.handbrakeApplied)
		{
			list.Add("<sprite name=\"HandbrakeWheel\"> Handbrake");
		}
		if (car.HasHotbox)
		{
			list.Add("<sprite name=\"Flame\"> Hotbox");
		}
		string result = (_cachedTooltipText = string.Join("\n", list));
		_cachedTooltipTextTime = unscaledTime;
		return result;
	}

	private static string PassengerString(Car car, PassengerMarker marker)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(car.PassengerCountString(marker) + " Passengers");
		HashSet<string> hashSet = (from g in marker.Groups
			where g.Count > 0
			select PassengerStop.ShortNameForIdentifier(g.Destination)).ToHashSet();
		if (hashSet.Count > 0)
		{
			stringBuilder.Append(" to " + string.Join(", ", hashSet));
		}
		return stringBuilder.ToString();
	}
}
