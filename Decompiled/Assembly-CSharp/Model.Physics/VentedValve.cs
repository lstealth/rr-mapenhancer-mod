using UnityEngine;

namespace Model.Physics;

public class VentedValve : AirConnection
{
	private float _valveState;

	private readonly AirConnection _vent;

	public VentedValve(Reservoir.Pipe pipe)
		: base(pipe)
	{
		_vent = new AirConnection(pipe);
	}

	public float ValveAutomaticBrake(Reservoir mainReservoir, Reservoir brakeLine, float psi, bool released, float dt)
	{
		return ValveVent(mainReservoir, brakeLine, psi, released, dt);
	}

	public float ValveVent(Reservoir input, Reservoir output, float psi, bool canValve, float dt)
	{
		float num = psi + 0.5f;
		float num2 = Mathf.Max(0f, psi - 0.5f);
		bool num3 = output.Pressure > num;
		bool flag = canValve && output.Pressure < num2 && input.Pressure >= num2;
		int num4 = ((!num3) ? ((!flag) ? 1 : 2) : 0);
		_valveState = num4;
		float num5 = Mathf.InverseLerp(1f, 0f, _valveState);
		float valve = Mathf.InverseLerp(1f, 2f, _valveState);
		Equalize(output, input, valve, dt, psi);
		float result = _vent.Equalize(output, null, num5, dt, psi);
		if (num5 > 0f && output.Pressure < psi)
		{
			output.Pressure = psi;
			_valveState = 1f;
		}
		return result;
	}
}
