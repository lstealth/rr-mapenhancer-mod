using Core;
using UnityEngine;

namespace RollingStock.PipeGenerator;

public struct Pipe
{
	public readonly BezierCurve Curve;

	public readonly float Radius;

	public Quaternion? RotationA;

	public Quaternion? RotationB;

	public Pipe(BezierCurve curve, float radius)
	{
		Curve = curve;
		Radius = radius;
		RotationA = null;
		RotationB = null;
	}
}
