using System;
using System.Collections.Generic;
using System.Linq;
using Model.Ops;
using Track;
using UnityEngine;

namespace UI.SwitchList;

public struct OpsCarListEntryDisplayable : IIndustryTrackDisplayable
{
	private readonly OpsCarList.Entry.Location _location;

	private readonly TrackSpan[] _trackSpans;

	public string DisplayName => _location.Title;

	public bool IsVisible => true;

	public IEnumerable<TrackSpan> TrackSpans => _trackSpans;

	public Vector3 CenterPoint => _location.Position;

	public OpsCarListEntryDisplayable(OpsCarList.Entry.Location location)
	{
		_location = location;
		Graph graph = Graph.Shared;
		_trackSpans = ((_location.SpanIds.Length == 0) ? Array.Empty<TrackSpan>() : _location.SpanIds.Select((string id) => graph.SpanForId(id)).ToArray());
	}
}
