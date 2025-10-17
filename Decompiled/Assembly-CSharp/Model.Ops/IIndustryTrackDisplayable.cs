using System.Collections.Generic;
using Track;
using UnityEngine;

namespace Model.Ops;

public interface IIndustryTrackDisplayable
{
	string DisplayName { get; }

	bool IsVisible { get; }

	IEnumerable<TrackSpan> TrackSpans { get; }

	Vector3 CenterPoint { get; }
}
