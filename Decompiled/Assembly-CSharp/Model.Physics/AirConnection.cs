using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Model.Physics;

public class AirConnection
{
	private readonly float _q;

	private static readonly Reservoir outside = new Reservoir("Outside", 1000000f, 0f);

	public const float ValveSpeedMultiplier = 10f;

	public float Velocity { get; private set; }

	public AirConnection(Reservoir.Pipe pipe)
	{
		_q = pipe switch
		{
			Reservoir.Pipe.Line => 1f, 
			Reservoir.Pipe.Feed => 0.4f, 
			Reservoir.Pipe.HalfInch => 0.3f, 
			_ => throw new ArgumentOutOfRangeException("pipe", pipe, null), 
		};
	}

	public override string ToString()
	{
		return $"{Velocity:F3}";
	}

	public float Equalize(Reservoir a, [CanBeNull] Reservoir b, float valve, float dt, float? bPressureOverride = null)
	{
		if (b == null)
		{
			b = outside;
		}
		float num = bPressureOverride ?? b.Pressure;
		float num2 = a.Pressure - num;
		float num3 = a.Volume / b.Volume;
		float num4 = num2 / (num3 + 1f);
		float num5 = Mathf.Sign(num2) * Mathf.InverseLerp(0f, 1f, Mathf.Abs(num2) * valve) * _q * 300f;
		if ((num5 > 0f && num5 > Velocity) || num5 < Velocity)
		{
			Velocity = Mathf.Lerp(Velocity, num5, dt * 10f);
		}
		else
		{
			Velocity = num5;
		}
		float num6 = ((!SameSign(num4, Velocity)) ? 0f : (Mathf.Sign(num4) * Mathf.Min(Mathf.Abs(num4), Mathf.Abs(Velocity * dt))));
		a.Pressure -= num6;
		b.Pressure += num6 * num3;
		if (a.Pressure < 0f)
		{
			a.Pressure = 0f;
		}
		if (b.Pressure < 0f)
		{
			b.Pressure = 0f;
		}
		outside.Pressure = 0f;
		return num6 / dt;
		static bool SameSign(float x, float y)
		{
			if (!(x <= 0f) || !(y <= 0f))
			{
				if (x >= 0f)
				{
					return y >= 0f;
				}
				return false;
			}
			return true;
		}
	}

	public float Valve(Reservoir source, Reservoir destination, float valve, float dt)
	{
		valve *= (float)((source.Pressure > destination.Pressure) ? 1 : 0);
		return Equalize(source, destination, valve, dt);
	}
}
