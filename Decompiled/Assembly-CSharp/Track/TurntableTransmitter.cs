using System.Collections;
using Game.Messages;
using Game.State;
using UnityEngine;

namespace Track;

public class TurntableTransmitter : MonoBehaviour
{
	public Turntable turntable;

	private Coroutine _transmitCoroutine;

	private static long Tick => StateManager.Now;

	private void Start()
	{
		StateManager.DebugAssertIsHost();
	}

	private void OnEnable()
	{
		if (StateManager.IsHost)
		{
			_transmitCoroutine = StartCoroutine(TransmitLoop());
		}
	}

	private void OnDisable()
	{
		if (_transmitCoroutine != null)
		{
			StopCoroutine(_transmitCoroutine);
		}
		_transmitCoroutine = null;
	}

	private IEnumerator TransmitLoop()
	{
		WaitForSeconds wait = new WaitForSeconds(0.2f);
		while (turntable == null)
		{
			yield return wait;
		}
		while (true)
		{
			float angle0 = turntable.Angle;
			while (turntable.StopIndex.HasValue || Mathf.Abs(Mathf.DeltaAngle(angle0, turntable.Angle)) < 0.001f)
			{
				angle0 = turntable.Angle;
				yield return wait;
			}
			StateManager.ApplyLocal(new TurntableUpdateStopIndex(turntable.id, Tick, turntable.Angle, turntable.StopIndex));
			do
			{
				angle0 = turntable.Angle;
				StateManager.ApplyLocal(new TurntableUpdateAngle(turntable.id, Tick, turntable.Angle));
				yield return wait;
			}
			while (Mathf.Abs(Mathf.DeltaAngle(angle0, turntable.Angle)) > 0.001f);
			StateManager.ApplyLocal(new TurntableUpdateStopIndex(turntable.id, Tick, turntable.Angle, turntable.StopIndex));
		}
	}
}
