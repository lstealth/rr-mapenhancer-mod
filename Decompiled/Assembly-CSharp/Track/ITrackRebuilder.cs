using System;
using Core;
using UnityEngine;

namespace Track;

public interface ITrackRebuilder
{
	Func<TrackObjectManager.ITrackDescriptor, GameObject> BuildGameObject { get; set; }

	Func<TrackObjectManager.ITrackDescriptor, GameObject> BuildMaskObject { get; set; }

	void Add(TrackObjectManager.ITrackDescriptor descriptor, BezierCurve curve);

	void Add(TrackObjectManager.ITrackDescriptor descriptor, BoundingSphere boundingSphere);

	void Remove(TrackObjectManager.ITrackDescriptor descriptor);

	void Clear();
}
