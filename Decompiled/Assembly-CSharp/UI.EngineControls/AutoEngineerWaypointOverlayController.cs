using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.Messages;
using Game.State;
using Model;
using Model.AI;
using Serilog;
using Track;
using UI.Tags;
using UnityEngine;

namespace UI.EngineControls;

public class AutoEngineerWaypointOverlayController : MonoBehaviour
{
	private static AutoEngineerWaypointOverlayController _shared;

	private List<Location> _locations;

	private bool _hasMoreLocations;

	private List<int> _currentArrowKeys = new List<int>();

	public static AutoEngineerWaypointOverlayController Shared
	{
		get
		{
			if (_shared == null)
			{
				_shared = Object.FindObjectOfType<AutoEngineerWaypointOverlayController>();
			}
			return _shared;
		}
	}

	private bool TagsVisible => TagController.Shared.TagsVisible;

	private BaseLocomotive Locomotive => TrainController.Shared.SelectedLocomotive;

	private static ArrowOverlayController ArrowOverlayController => ArrowOverlayController.Shared;

	private void OnEnable()
	{
		Messenger.Default.Register<TagVisibilityDidChange>(this, OnTagVisibilityChanged);
	}

	private void OnDisable()
	{
		Messenger.Default.Unregister(this);
		Clear();
	}

	private void ShowRouteOverlay()
	{
		if (_locations == null)
		{
			return;
		}
		Log.Debug("Waypoint Overlay: Show: {locCount}", _locations.Count);
		HideRouteOverlay();
		Color color = new Color(0.75f, 0.35f, 0.3f);
		Color color2 = new Color(0.45f, 0.8f, 0.25f);
		Graph shared = Graph.Shared;
		ArrowOverlayController arrowOverlayController = ArrowOverlayController;
		foreach (Location location in _locations)
		{
			List<Location> locations = _locations;
			bool num = location.Equals(locations[locations.Count - 1]);
			Graph.PositionRotation positionRotation = shared.GetPositionRotation(location);
			Vector3 position = positionRotation.Position;
			Quaternion rotation = positionRotation.Rotation;
			Vector3 vector = position;
			Quaternion quaternion = rotation;
			Vector3 position2;
			Quaternion rotation2;
			Color color3;
			if (num)
			{
				position2 = vector;
				rotation2 = quaternion;
				color3 = color;
			}
			else
			{
				rotation2 = quaternion * Quaternion.Euler(-90f, 0f, 0f);
				position2 = vector + Vector3.up * 0.25f + quaternion * Vector3.forward * 1f;
				color3 = color2;
			}
			int item = arrowOverlayController.AddArrow(position2, rotation2, color3, 0.8f, animated: false);
			_currentArrowKeys.Add(item);
		}
	}

	private void HideRouteOverlay()
	{
		if (!(ArrowOverlayController == null))
		{
			Log.Debug("Waypoint Overlay: Hide");
			ArrowOverlayController.RemoveArrows(_currentArrowKeys, animated: false);
			_currentArrowKeys.Clear();
		}
	}

	public void Clear()
	{
		HideRouteOverlay();
		_locations = null;
	}

	private void OnTagVisibilityChanged(TagVisibilityDidChange change)
	{
		if (change.IsVisible)
		{
			if (!(Locomotive == null))
			{
				if (_locations != null)
				{
					ShowRouteOverlay();
				}
				RequestRoute();
			}
		}
		else
		{
			HideRouteOverlay();
		}
	}

	public void WaypointDidChange(OrderWaypoint? ordersWaypoint)
	{
		_locations = null;
		HideRouteOverlay();
		if (TagsVisible)
		{
			RequestRoute();
		}
	}

	public void WaypointRouteDidUpdate(Location? maybeCurrentStepLocation, bool routeChanged)
	{
		if (!TagsVisible)
		{
			return;
		}
		if (routeChanged)
		{
			RequestRoute();
		}
		else
		{
			if (!maybeCurrentStepLocation.HasValue)
			{
				return;
			}
			Location valueOrDefault = maybeCurrentStepLocation.GetValueOrDefault();
			if (_locations == null)
			{
				return;
			}
			for (int i = 0; i < _locations.Count; i++)
			{
				if (_locations[i].Equals(valueOrDefault))
				{
					_locations.RemoveRange(0, i + 1);
					ShowRouteOverlay();
					if (_hasMoreLocations && _locations.Count < 5)
					{
						RequestRoute();
					}
					return;
				}
			}
			Log.Warning("Waypoint Overlay: WaypointStepDidChange NOT FOUND");
		}
	}

	private void RequestRoute()
	{
		Log.Debug("Waypoint Overlay: RequestRoute");
		StateManager.ApplyLocal(new AutoEngineerWaypointRouteRequest(Locomotive.id));
	}

	public void DidReceiveRoute(string locomotiveId, List<Location> locations, bool responseHasMore)
	{
		Log.Debug("Waypoint Overlay: DidReceiveRoute {count}", locations.Count);
		if (!(locomotiveId != Locomotive?.id))
		{
			_locations = locations.ToList();
			_hasMoreLocations = responseHasMore;
			if (TagsVisible)
			{
				ShowRouteOverlay();
			}
		}
	}
}
