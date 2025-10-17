using System;

namespace Track.Signals;

[Serializable]
public enum SignalAspect
{
	Stop,
	Approach,
	Clear,
	DivergingApproach,
	DivergingClear,
	Restricting
}
