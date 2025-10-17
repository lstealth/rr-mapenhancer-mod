using System.Collections.Generic;

namespace Model;

public class CarSegmentCache
{
	private readonly TrainController _trainController;

	private readonly Dictionary<string, List<string>> _segmentIdToCarIds = new Dictionary<string, List<string>>();

	private readonly Dictionary<string, List<string>> _carIdToSegmentIds = new Dictionary<string, List<string>>();

	private static readonly List<string> Empty = new List<string>();

	public CarSegmentCache(TrainController trainController)
	{
		_trainController = trainController;
	}

	public void UpdateCarPosition(string carId, string segmentIdA, string segmentIdB)
	{
		RemoveCarSegments(carId, removeRecord: false);
		if (!_segmentIdToCarIds.TryGetValue(segmentIdA, out var value))
		{
			_segmentIdToCarIds.Add(segmentIdA, value = new List<string>());
		}
		if (!_segmentIdToCarIds.TryGetValue(segmentIdB, out var value2))
		{
			_segmentIdToCarIds.Add(segmentIdB, value2 = new List<string>());
		}
		if (!value.Contains(carId))
		{
			value.Add(carId);
		}
		if (!value2.Contains(carId))
		{
			value2.Add(carId);
		}
		if (!_carIdToSegmentIds.TryGetValue(carId, out var value3))
		{
			_carIdToSegmentIds.Add(carId, value3 = new List<string>());
		}
		if (!value3.Contains(segmentIdA))
		{
			value3.Add(segmentIdA);
		}
		if (!value3.Contains(segmentIdB))
		{
			value3.Add(segmentIdB);
		}
	}

	public void RemoveCarSegments(string carId, bool removeRecord)
	{
		if (!_carIdToSegmentIds.TryGetValue(carId, out var value))
		{
			return;
		}
		foreach (string item in value)
		{
			_segmentIdToCarIds[item].Remove(carId);
		}
		value.Clear();
		if (removeRecord)
		{
			_carIdToSegmentIds.Remove(carId);
		}
	}

	public IReadOnlyList<string> EnumerateCarIdsOnSegment(string segmentId)
	{
		return _segmentIdToCarIds.GetValueOrDefault(segmentId, Empty);
	}

	public void Clear()
	{
		_carIdToSegmentIds.Clear();
		_segmentIdToCarIds.Clear();
	}
}
