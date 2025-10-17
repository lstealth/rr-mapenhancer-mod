using System;
using UnityEngine;

namespace Model.AI;

[Serializable]
public struct FloatRange
{
	[SerializeField]
	public float minimum;

	[SerializeField]
	public float maximum;

	public FloatRange(float minimum, float maximum)
	{
		this.minimum = minimum;
		this.maximum = maximum;
	}

	public override string ToString()
	{
		return $"{minimum:F3} - {maximum:F3}";
	}
}
