using System;
using Model;
using UnityEngine;

namespace Cameras;

public readonly struct JumpTarget
{
	public readonly Vector3 Position;

	public readonly Quaternion Rotation;

	public readonly float RandomRadius;

	public readonly Car RelativeToCar;

	public readonly bool IsRelativeToCar;

	public JumpTarget(Vector3 position, Quaternion rotation, float randomRadius = 0f)
	{
		Position = position;
		Rotation = rotation;
		RandomRadius = randomRadius;
		RelativeToCar = null;
		IsRelativeToCar = false;
	}

	public JumpTarget(Vector3 position, Quaternion rotation, Car relativeToCar)
	{
		Position = position;
		Rotation = rotation;
		RandomRadius = 0f;
		RelativeToCar = relativeToCar;
		IsRelativeToCar = true;
	}

	public bool Equals(JumpTarget other)
	{
		if (Position.Equals(other.Position) && Rotation.Equals(other.Rotation))
		{
			float randomRadius = RandomRadius;
			if (randomRadius.Equals(other.RandomRadius) && object.Equals(RelativeToCar, other.RelativeToCar))
			{
				return object.Equals(IsRelativeToCar, other.IsRelativeToCar);
			}
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is JumpTarget other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Position, Rotation, RandomRadius, IsRelativeToCar);
	}
}
