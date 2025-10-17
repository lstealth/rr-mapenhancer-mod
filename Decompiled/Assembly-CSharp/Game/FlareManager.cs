using System;
using System.Collections.Generic;
using Effects;
using GalaSoft.MvvmLight.Messaging;
using Game.Messages;
using Game.State;
using Helpers;
using KeyValue.Runtime;
using Network;
using Track;
using UnityEngine;

namespace Game;

public class FlareManager : MonoBehaviour
{
	public Effects.Flare flarePrefab;

	private KeyValueObject _keyValueObject;

	private IDisposable _keyChangeObserver;

	private readonly Dictionary<string, IDisposable> _keyObservers = new Dictionary<string, IDisposable>();

	private readonly Dictionary<string, GameObject> _instances = new Dictionary<string, GameObject>();

	private const string KeyLocation = "loc";

	private const string KeyType = "type";

	private const string KeyCreator = "by";

	public static FlareManager Shared { get; private set; }

	private void OnEnable()
	{
		_keyValueObject = base.gameObject.GetComponent<KeyValueObject>();
		_keyChangeObserver = _keyValueObject.ObserveKeyChanges(KeyDidChange);
		Shared = this;
	}

	private void OnDisable()
	{
		Shared = null;
		_keyChangeObserver.Dispose();
		UnityEngine.Object.Destroy(_keyValueObject);
	}

	public void AddFlare(Location location, IPlayer sender)
	{
		StateManager.AssertIsHost();
		foreach (FlarePickable item in NearbyFlares(location))
		{
			RemoveFlare(item.FlareId, sender);
		}
		if (Graph.Shared.IsWithinFoulingDistance(location))
		{
			Multiplayer.SendError(sender, "Too close to switch.");
			return;
		}
		string key = string.Format("{0}-{1}-{2}", location.segment.id, (int)location.distance, location.EndIsA ? "a" : "b");
		Dictionary<string, Value> value = new Dictionary<string, Value>
		{
			{
				"type",
				Value.String("flare")
			},
			{
				"loc",
				location.PropertyValue()
			},
			{
				"by",
				Value.String(sender.PlayerId.String)
			}
		};
		_keyValueObject[key] = Value.Dictionary(value);
	}

	public void RemoveFlare(string id, IPlayer sender)
	{
		StateManager.AssertIsHost();
		_keyValueObject[id] = Value.Null();
	}

	private void KeyDidChange(string key, KeyChange change)
	{
		switch (change)
		{
		case KeyChange.Add:
			_keyObservers[key] = _keyValueObject.Observe(key, delegate(Value value2)
			{
				HandleAddUpdateFlare(key, value2);
			});
			break;
		case KeyChange.Remove:
		{
			HandleRemoveFlare(key);
			if (_keyObservers.TryGetValue(key, out var value))
			{
				value.Dispose();
			}
			_keyObservers.Remove(key);
			break;
		}
		default:
			throw new ArgumentOutOfRangeException("change", change, null);
		}
	}

	private void HandleAddUpdateFlare(string key, Value value)
	{
		HandleRemoveFlare(key);
		if (!value.IsNull)
		{
			PlayerId placedBy = new PlayerId(value["by"].StringValue);
			Graph graph = TrainController.Shared.graph;
			Location location = graph.LocationFrom(value["loc"]);
			Vector3 v = graph.GetPosition(location) + Vector3.down * Gauge.Standard.RailHeight;
			GameObject gameObject = new GameObject
			{
				name = "Flare " + key,
				hideFlags = HideFlags.DontSave
			};
			gameObject.transform.SetParent(base.transform, worldPositionStays: false);
			gameObject.transform.position = WorldTransformer.GameToWorld(v);
			gameObject.SetActive(value: false);
			TrackMarker trackMarker = gameObject.AddComponent<TrackMarker>();
			trackMarker.type = TrackMarkerType.Flare;
			trackMarker.Location = location;
			Transform obj = UnityEngine.Object.Instantiate(flarePrefab, gameObject.transform, worldPositionStays: false).transform;
			obj.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(-180, 180), 0f);
			obj.GetComponentInChildren<FlarePickable>().Configure(key, placedBy);
			gameObject.SetActive(value: true);
			_instances[key] = gameObject;
			Messenger.Default.Send(new FlareAdded(key, location));
		}
	}

	private void HandleRemoveFlare(string key)
	{
		if (_instances.TryGetValue(key, out var value))
		{
			UnityEngine.Object.Destroy(value);
			_instances.Remove(key);
		}
	}

	public void PlaceFlare(Camera theCamera)
	{
		Location? location = TrainController.Shared.graph.LocationFromMouse(theCamera);
		if (location.HasValue)
		{
			StateManager.ApplyLocal(new FlareAddUpdate(Graph.CreateSnapshotTrackLocation(location.Value)));
		}
	}

	public static bool TryGetFlarePickable(TrackMarker trackMarker, out FlarePickable flarePickable)
	{
		if (trackMarker.type != TrackMarkerType.Flare)
		{
			flarePickable = null;
			return false;
		}
		flarePickable = trackMarker.GetComponentInChildren<FlarePickable>();
		return flarePickable != null;
	}

	private static IEnumerable<FlarePickable> NearbyFlares(Location location, float radius = 20f)
	{
		Graph graph = Graph.Shared;
		foreach (TrackMarker item in graph.EnumerateTrackMarkers(location, radius, sameDirection: false))
		{
			if (TryGetFlarePickable(item, out var flarePickable))
			{
				yield return flarePickable;
			}
		}
		foreach (TrackMarker item2 in graph.EnumerateTrackMarkers(location, 0f - radius, sameDirection: false))
		{
			if (TryGetFlarePickable(item2, out var flarePickable2))
			{
				yield return flarePickable2;
			}
		}
	}
}
