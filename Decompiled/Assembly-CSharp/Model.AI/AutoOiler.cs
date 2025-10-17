using System.Collections;
using System.Collections.Generic;
using Game;
using Game.State;
using Serilog;
using UnityEngine;

namespace Model.AI;

public class AutoOiler : MonoBehaviour
{
	private Coroutine _coroutine;

	private IReadOnlyList<Car> _cars = new List<Car>();

	private bool _reverse;

	private Car _originCar;

	private float _pendingRunDuration;

	private int _oiledCount;

	private const float StartDelay = 30f;

	private const float TimeToFullyOil = 10f;

	private const float TimeToWalkCar = 10f;

	private const float OilIfBelow = 0.75f;

	public void Configure(Car originCar, IReadOnlyList<Car> cars)
	{
		_originCar = originCar;
		_cars = cars ?? new List<Car>();
	}

	public void SetStopped(bool stopped)
	{
		bool flag = stopped && Car.OilFeature;
		bool flag2 = _coroutine != null;
		if (flag != flag2)
		{
			if (flag)
			{
				_coroutine = StartCoroutine(Loop());
				return;
			}
			Log.Debug("AutoOiler {name} stopping.", base.name);
			StopCoroutine(_coroutine);
			_coroutine = null;
			PayWages();
		}
	}

	private int NextIndex(int index)
	{
		if (!_reverse)
		{
			return index + 1;
		}
		return index - 1;
	}

	private bool InBounds(int index)
	{
		if (index >= 0)
		{
			return index < _cars.Count;
		}
		return false;
	}

	private IEnumerator Loop()
	{
		int originIndex = FindOriginIndex();
		if (originIndex < 0)
		{
			Log.Error("Couldn't find origin car {car}", _originCar);
			_coroutine = null;
			yield break;
		}
		_reverse = originIndex > _cars.Count - originIndex;
		Log.Debug("AutoOiler {name} starting, rev = {reverse}", base.name, _reverse);
		while (true)
		{
			yield return new WaitForSeconds(30f);
			int carIndex = originIndex;
			do
			{
				if (TryGetCar(carIndex, out var car))
				{
					float num = 0f;
					if (car.NeedsOiling && car.Oiled < 0.75f)
					{
						float num2 = 1f - car.Oiled;
						car.OffsetOiled(num2);
						float num3 = num2 * 10f;
						num += num3;
						_pendingRunDuration += num3;
						_oiledCount++;
						Log.Verbose("AutoOiler {name}: oiled {car}", base.name, car);
					}
					num += 10f;
					_pendingRunDuration += 10f;
					yield return new WaitForSeconds(num);
				}
				carIndex = NextIndex(carIndex);
			}
			while (InBounds(carIndex));
			_reverse = !_reverse;
			PayWages();
		}
	}

	private void PayWages()
	{
		if (_oiledCount > 0)
		{
			Log.Verbose("AutoOiler {name}: Billing {duration} (unmult) for {count} cars oiled.", base.name, _pendingRunDuration, _oiledCount);
			StateManager.Shared.RecordAutoEngineerRunDuration(_pendingRunDuration * TimeWeather.TimeMultiplier);
		}
		_pendingRunDuration = 0f;
		_oiledCount = 0;
	}

	private int FindOriginIndex()
	{
		for (int i = 0; i < _cars.Count; i++)
		{
			if (_cars[i] == _originCar)
			{
				return i;
			}
		}
		return -1;
	}

	private bool TryGetCar(int carIndex, out Car car)
	{
		car = null;
		if (_cars == null)
		{
			return false;
		}
		if (carIndex < 0 || carIndex >= _cars.Count)
		{
			return false;
		}
		car = _cars[carIndex];
		return car != null;
	}
}
