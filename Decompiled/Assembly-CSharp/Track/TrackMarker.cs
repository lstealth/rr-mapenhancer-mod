using System;
using Model.Ops;
using Serilog;
using Track.Signals;
using UnityEngine;

namespace Track;

public class TrackMarker : MonoBehaviour
{
	public string id;

	public TrackMarkerType type;

	[SerializeField]
	[HideInInspector]
	private SerializableLocation _location;

	private Graph.PositionRotation? _cachedPosition;

	private bool _warnedInvalid;

	private CTCSignal _signal;

	private PassengerStop _passengerStop;

	public Location? Location
	{
		get
		{
			if (Graph == null)
			{
				return null;
			}
			if (!_location.IsValid)
			{
				return null;
			}
			return Graph.MakeLocation(_location);
		}
		set
		{
			_location = value?.Serializable() ?? default(SerializableLocation);
			InvalidateCache();
			this.OnLocationChanged?.Invoke();
		}
	}

	internal Graph Graph => Graph.Shared;

	public static Color ColorEdit => Color.red;

	public static Color ColorLower => Color.magenta;

	public Graph.PositionRotation? PositionRotation
	{
		get
		{
			UpdateCachedPointsIfNeeded();
			return _cachedPosition;
		}
	}

	public CTCSignal Signal
	{
		get
		{
			if (type != TrackMarkerType.Signal)
			{
				return null;
			}
			if (_signal == null)
			{
				_signal = GetComponent<CTCSignal>();
			}
			return _signal;
		}
	}

	public PassengerStop PassengerStop
	{
		get
		{
			if (type != TrackMarkerType.PassengerStop)
			{
				return null;
			}
			if (_passengerStop == null)
			{
				_passengerStop = GetComponentInParent<PassengerStop>();
			}
			return _passengerStop;
		}
	}

	public event Action OnLocationChanged;

	private void OnEnable()
	{
		Graph.RegisterTrackMarker(this);
	}

	private void OnDisable()
	{
		Graph graph = Graph;
		if (graph != null)
		{
			graph.UnregisterTrackMarker(this);
		}
	}

	private void OnDrawGizmosSelected()
	{
		UpdateCachedPointsIfNeeded();
		DrawGizmosEndpoints();
	}

	private void DrawGizmosEndpoints()
	{
	}

	public void InvalidateCache()
	{
		_cachedPosition = null;
	}

	private void UpdateCachedPointsIfNeeded()
	{
		if (_cachedPosition.HasValue)
		{
			return;
		}
		Location? location = Location;
		if (!location.HasValue || !location.Value.IsValid)
		{
			if (!(Graph == null) && Graph.HasPopulatedCollections && !_warnedInvalid)
			{
				Log.Warning("Marker {name} {id} missing point or invalid", base.name, id);
				_warnedInvalid = true;
			}
		}
		else
		{
			_cachedPosition = Graph.GetPositionRotation(location.Value);
			_warnedInvalid = false;
		}
	}

	public void Flip()
	{
		Location = Location?.Flipped();
	}
}
