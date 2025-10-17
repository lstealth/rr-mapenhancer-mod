using System.Collections.Generic;

namespace RollingStock;

public readonly struct MultiGauge : IGauge
{
	private readonly List<IGauge> _gauges;

	public float Value
	{
		get
		{
			if (_gauges != null && _gauges.Count != 0)
			{
				return _gauges[0].Value;
			}
			return 0f;
		}
		set
		{
			if (_gauges == null)
			{
				return;
			}
			foreach (IGauge gauge in _gauges)
			{
				gauge.Value = value;
			}
		}
	}

	public MultiGauge(List<IGauge> gauges)
	{
		_gauges = gauges;
	}
}
