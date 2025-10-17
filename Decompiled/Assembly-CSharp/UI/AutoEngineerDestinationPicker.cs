using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using Model;
using Model.AI;
using Serilog;
using Track;
using UI.Common;
using UI.EngineControls;
using UnityEngine;
using UnityEngine.Pool;

namespace UI;

public class AutoEngineerDestinationPicker : MonoBehaviour
{
	private struct Hit
	{
		public Location Location;

		public (Car car, Car.End end)? CarInfo;

		public Hit(Location location, (Car car, Car.End end)? carInfo)
		{
			Location = location;
			CarInfo = carInfo;
		}
	}

	private static AutoEngineerDestinationPicker _shared;

	[SerializeField]
	private Transform destinationMarker;

	private BaseLocomotive _locomotive;

	private Camera _camera;

	private Coroutine _coroutine;

	private Graph _graph;

	private AutoEngineerOrdersHelper _ordersHelper;

	private HashSet<Car> _dontSnapToCars;

	public static AutoEngineerDestinationPicker Shared
	{
		get
		{
			if (_shared == null)
			{
				_shared = Object.FindObjectOfType<AutoEngineerDestinationPicker>();
			}
			return _shared;
		}
	}

	public bool MouseClicked
	{
		get
		{
			if (GameInput.IsMouseOverUI(out var _, out var _))
			{
				return false;
			}
			return GameInput.shared.PrimaryPressEndedThisFrame;
		}
	}

	public void StartPickingLocation(BaseLocomotive locomotive, AutoEngineerOrdersHelper ordersHelper)
	{
		_locomotive = locomotive;
		if ((object)_graph == null)
		{
			_graph = Graph.Shared;
		}
		_ordersHelper = ordersHelper;
		_dontSnapToCars = locomotive.EnumerateCoupled().ToHashSet();
		if (_coroutine != null)
		{
			StopCoroutine(_coroutine);
		}
		_coroutine = StartCoroutine(Loop());
		ShowMessage("Click to set waypoint for " + locomotive.DisplayName);
		Location? currentOrdersGotoLocation = GetCurrentOrdersGotoLocation();
		if (currentOrdersGotoLocation.HasValue)
		{
			currentOrdersGotoLocation.GetValueOrDefault();
		}
		GameInput.RegisterEscapeHandler(GameInput.EscapeHandler.Transient, DidEscape);
	}

	public void Cancel()
	{
		if (_coroutine != null)
		{
			ShowMessage("Cancelled waypoint selection");
			StopLoop();
		}
	}

	private Location? GetCurrentOrdersGotoLocation()
	{
		OrderWaypoint? waypoint = _ordersHelper.Orders.Waypoint;
		if (waypoint.HasValue)
		{
			OrderWaypoint valueOrDefault = waypoint.GetValueOrDefault();
			return _graph.ResolveLocationString(valueOrDefault.LocationString);
		}
		return null;
	}

	private bool DidEscape()
	{
		Cancel();
		return true;
	}

	private void StopLoop()
	{
		if (_coroutine != null)
		{
			StopCoroutine(_coroutine);
			_coroutine = null;
		}
		GameInput.UnregisterEscapeHandler(GameInput.EscapeHandler.Transient);
		destinationMarker.gameObject.SetActive(value: false);
	}

	private void ShowMessage(string message)
	{
		Toast.Present(message, ToastPosition.Bottom);
	}

	private IEnumerator Loop()
	{
		Hit valueOrDefault;
		Location location;
		while (true)
		{
			Location? currentOrdersGotoLocation = GetCurrentOrdersGotoLocation();
			Hit? hit = HitLocation();
			if (hit.HasValue)
			{
				valueOrDefault = hit.GetValueOrDefault();
				location = valueOrDefault.Location;
				Graph.PositionRotation positionRotation = _graph.GetPositionRotation(location);
				destinationMarker.position = WorldTransformer.GameToWorld(positionRotation.Position);
				destinationMarker.rotation = positionRotation.Rotation;
				destinationMarker.gameObject.SetActive(value: true);
				if (!currentOrdersGotoLocation.Equals(location) && MouseClicked)
				{
					break;
				}
			}
			else
			{
				destinationMarker.gameObject.SetActive(value: false);
			}
			yield return null;
		}
		Log.Debug("DestinationPicker Hit: {hit} {car} {end}", valueOrDefault.Location, valueOrDefault.CarInfo?.car, valueOrDefault.CarInfo?.end);
		_ordersHelper.SetWaypoint(location, valueOrDefault.CarInfo?.car.id);
		StopLoop();
	}

	private Hit? HitLocation()
	{
		if (!MainCameraHelper.TryGetIfNeeded(ref _camera))
		{
			return null;
		}
		Location? location = _graph.LocationFromMouse(_camera);
		if (location.HasValue)
		{
			Location valueOrDefault = location.GetValueOrDefault();
			TrainController shared = TrainController.Shared;
			Vector3 position = _graph.GetPosition(valueOrDefault);
			float num = 2f;
			Hit? result = null;
			HashSet<Car> value;
			using (CollectionPool<HashSet<Car>, Car>.Get(out value))
			{
				shared.CheckForCarsAtPoint(position, 2f, value, valueOrDefault);
				foreach (Car item in value)
				{
					if (_dontSnapToCars.Contains(item))
					{
						continue;
					}
					if (!item[item.EndToLogical(Car.End.F)].IsCoupled)
					{
						Location location2 = _graph.LocationByMoving(item.LocationF, 0.5f, checkSwitchAgainstMovement: false, stopAtEndOfTrack: true);
						float distanceBetweenClose = _graph.GetDistanceBetweenClose(valueOrDefault, location2);
						if (distanceBetweenClose < num)
						{
							num = distanceBetweenClose;
							result = new Hit(location2, (item, Car.End.F));
						}
					}
					if (!item[item.EndToLogical(Car.End.R)].IsCoupled)
					{
						Location location3 = _graph.LocationByMoving(item.LocationR, -0.5f, checkSwitchAgainstMovement: false, stopAtEndOfTrack: true).Flipped();
						float distanceBetweenClose2 = _graph.GetDistanceBetweenClose(valueOrDefault, location3);
						if (distanceBetweenClose2 < num)
						{
							num = distanceBetweenClose2;
							result = new Hit(location3, (item, Car.End.R));
						}
					}
				}
				if (value.Count > 0)
				{
					return result;
				}
			}
			return new Hit(valueOrDefault, null);
		}
		return null;
	}
}
