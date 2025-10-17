using System;
using UnityEngine;

namespace Model.AI;

[Serializable]
public class PIDController
{
	[Range(-4f, 4f)]
	[SerializeField]
	private float proportionalGain = 2f;

	[Range(-4f, 4f)]
	[SerializeField]
	public float integralGain = 0.5f;

	[Range(-4f, 4f)]
	[SerializeField]
	public float derivativeGain = 0.25f;

	[Range(-2f, 2f)]
	[SerializeField]
	public float integralGrowth = 1f;

	[Range(0f, 2f)]
	[SerializeField]
	private float integralDecay;

	private float _previousError;

	private float _integrator;

	private float _previousControl;

	private float _previousDerivative;

	private float _previousIntegrator;

	public FloatRange errorRange = new FloatRange(-100f, 100f);

	public FloatRange integratorRange = new FloatRange(-100f, 100f);

	public FloatRange outputRange = new FloatRange(-100f, 100f);

	[SerializeField]
	private float maximumStep = 100f;

	public float PreviousError => _previousError;

	public float Integrator => _integrator;

	public float DebugIntegrator => _previousIntegrator;

	public float DebugDerivative => _previousDerivative;

	public PIDController(float kp, float ki, float kd)
	{
		proportionalGain = kp;
		integralGain = ki;
		derivativeGain = kd;
	}

	public PIDController()
	{
	}

	public float Compute(float error, float dt)
	{
		if (dt == 0f)
		{
			dt = 1f;
		}
		error = error.Clamp(errorRange);
		_integrator *= 1f - integralDecay * dt;
		_integrator = (_integrator + integralGrowth * error * dt).Clamp(integratorRange);
		float num = proportionalGain * error;
		float num2 = derivativeGain * ((error - _previousError) / dt);
		float num3 = integralGain * _integrator;
		float result = (_previousControl = (num + num3 + num2).Clamp(outputRange).ClampToMaximumStep(_previousControl, maximumStep));
		_previousError = error;
		_previousDerivative = num2;
		_previousIntegrator = num3;
		return result;
	}

	public void Reset()
	{
		_integrator = 0f;
		_previousError = 0f;
		_previousControl = 0f;
	}

	public void CopyTo(PIDController other)
	{
		other.proportionalGain = proportionalGain;
		other.integralGain = integralGain;
		other.derivativeGain = derivativeGain;
		other.errorRange = errorRange;
		other.outputRange = outputRange;
		other.integratorRange = integratorRange;
		other.integralDecay = integralDecay;
		other.integralGrowth = integralGrowth;
		other.maximumStep = maximumStep;
	}
}
