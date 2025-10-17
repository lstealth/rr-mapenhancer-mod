using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Helpers;
using Serilog;
using UnityEngine;

namespace Track;

[ExecuteInEditMode]
[RequireComponent(typeof(TrackMarker))]
public class TrackRelativePosition : MonoBehaviour
{
	[SerializeField]
	private Transform targetTransform;

	[SerializeField]
	private Vector3 targetOffset;

	[SerializeField]
	private float targetAngle;

	[SerializeField]
	private bool snapToTerrain;

	[Tooltip("True if the target game object should be activated or deactivated based on the associated track group id.")]
	[SerializeField]
	private bool targetActiveFollowsTrackGroup;

	private TrackMarker _marker;

	private void Awake()
	{
		_marker = GetComponent<TrackMarker>();
		if (targetActiveFollowsTrackGroup)
		{
			ObserveTrackGroup();
		}
	}

	private void OnDestroy()
	{
		Messenger.Default.Unregister(this);
	}

	[ContextMenu("Apply To Target")]
	private void Apply()
	{
		if (_marker == null)
		{
			return;
		}
		Graph.PositionRotation? positionRotation = _marker.PositionRotation;
		if (!positionRotation.HasValue || !(targetTransform != null))
		{
			return;
		}
		Graph.PositionRotation value = positionRotation.Value;
		Vector3 vector = WorldTransformer.GameToWorld(value.Position) + value.Rotation * targetOffset;
		if (snapToTerrain)
		{
			if (!Physics.Raycast(new Ray(vector + Vector3.up * 10f, Vector3.down), out var hitInfo, 20f, 1 << Layers.Terrain))
			{
				Debug.LogWarning("Could not snap to ground: no ground (is terrain loaded here?)", this);
				return;
			}
			vector = hitInfo.point;
		}
		Quaternion rotation = Quaternion.Euler(0f, targetAngle, 0f) * value.Rotation;
		targetTransform.position = vector;
		targetTransform.rotation = rotation;
	}

	private void ObserveTrackGroup()
	{
		if (!Application.isPlaying)
		{
			return;
		}
		Location? location = _marker.Location;
		if (!location.HasValue || location.Value.segment == null)
		{
			Log.Warning("TrackMarker {marker} location is null or null segment: {markerLocation}", _marker, location);
		}
		else if (!string.IsNullOrEmpty(location.Value.segment.groupId))
		{
			string groupId = location.Value.segment.groupId;
			UpdateTransformActiveForGroup(groupId);
			Messenger.Default.Register<GraphDidChangeAvailableGroups>(this, delegate
			{
				UpdateTransformActiveForGroup(groupId);
			});
		}
	}

	private void UpdateTransformActiveForGroup(string groupId)
	{
		Graph shared = Graph.Shared;
		targetTransform.gameObject.SetActive(shared.availableGroupIds.Contains(groupId));
	}
}
