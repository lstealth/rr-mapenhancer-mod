using System.Collections.Generic;
using Model.Ops;
using Track;
using UnityEngine;

namespace UI.CarInspector;

public struct OpsCarPositionDisplayable : IIndustryTrackDisplayable
{
	private readonly OpsCarPosition _position;

	public string DisplayName => _position.DisplayName;

	public bool IsVisible => true;

	public IEnumerable<TrackSpan> TrackSpans => _position.Spans;

	public Vector3 CenterPoint => _position.GetCenter();

	public OpsCarPositionDisplayable(OpsCarPosition position)
	{
		_position = position;
	}
}
