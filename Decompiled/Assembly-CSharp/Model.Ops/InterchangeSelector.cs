using System;
using System.Linq;
using Track;
using UnityEngine;

namespace Model.Ops;

internal class InterchangeSelector
{
	private OpsCarPosition? _lastPosition;

	private int _carCapacity;

	private int _carCount;

	private int _interchangeIntervalIndex;

	private int _interchangeIndex;

	private static readonly float[] InterchangeIntervals = new float[8] { 0.5f, 0.5f, 0.3f, 0.7f, 0.6f, 0.4f, 0.7f, 0.3f };

	public Interchange InterchangeForPosition(OpsCarPosition position, Interchange[] interchanges)
	{
		if (interchanges.Length == 0)
		{
			throw new Exception("No interchanges");
		}
		if (interchanges.Length == 1)
		{
			return interchanges[0];
		}
		if (!_lastPosition.HasValue || !_lastPosition.Value.Equals(position))
		{
			_carCapacity = Mathf.RoundToInt(position.Spans.Sum((TrackSpan span) => span.Length / 12.192f));
			_carCount = 0;
			_lastPosition = position;
			_interchangeIntervalIndex++;
			_interchangeIndex++;
		}
		float num = InterchangeIntervals[_interchangeIntervalIndex % InterchangeIntervals.Length];
		int num2 = Mathf.CeilToInt(0.25f * num * (float)_carCapacity);
		Interchange result = interchanges[_interchangeIndex % interchanges.Length];
		_carCount++;
		if (_carCount > num2)
		{
			_interchangeIntervalIndex++;
			_interchangeIndex++;
			_carCount = 0;
		}
		return result;
	}
}
