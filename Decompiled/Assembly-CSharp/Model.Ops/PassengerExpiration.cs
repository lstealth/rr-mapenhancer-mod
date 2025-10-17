using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using Game;
using Game.Events;
using Game.State;
using Serilog;
using UnityEngine;

namespace Model.Ops;

public class PassengerExpiration : GameBehaviour
{
	private Coroutine _coroutine;

	public const int ExpirationTimeInGameHours = 4;

	protected override void OnEnableWithProperties()
	{
		if (StateManager.IsHost)
		{
			_coroutine = StartCoroutine(Loop());
			Messenger.Default.Register<TimeAdvanced>(this, delegate
			{
				TickAndLogException();
			});
		}
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		Messenger.Default.Unregister(this);
		if (_coroutine != null)
		{
			StopCoroutine(_coroutine);
			_coroutine = null;
		}
	}

	private IEnumerator Loop()
	{
		WaitForSeconds wait = new WaitForSeconds(60f);
		while (true)
		{
			TickAndLogException();
			yield return wait;
		}
	}

	private void TickAndLogException()
	{
		try
		{
			Tick();
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Error during loop");
		}
		finally
		{
		}
	}

	private void Tick()
	{
		IEnumerable<PassengerStop> enumerable = PassengerStop.FindAll();
		List<Car> list = AllPassengerCars();
		GameDateTime gameDateTime = TimeWeather.Now.AddingHours(-4f);
		using (StateManager.TransactionScope())
		{
			int num = 0;
			foreach (PassengerStop item in enumerable)
			{
				num += item.ExpirePassengers(gameDateTime);
			}
			foreach (Car item2 in list)
			{
				PassengerMarker? passengerMarker = item2.GetPassengerMarker();
				if (!passengerMarker.HasValue)
				{
					continue;
				}
				PassengerMarker valueOrDefault = passengerMarker.GetValueOrDefault();
				bool flag = false;
				for (int num2 = valueOrDefault.Groups.Count - 1; num2 >= 0; num2--)
				{
					PassengerGroup passengerGroup = valueOrDefault.Groups[num2];
					if (!(passengerGroup.Boarded >= gameDateTime))
					{
						num += passengerGroup.Count;
						valueOrDefault.Groups.RemoveAt(num2);
						flag = true;
					}
				}
				if (flag)
				{
					item2.SetPassengerMarker(valueOrDefault);
				}
			}
			if (num > 0)
			{
				Log.Information("Expired {count} passengers since {exp}.", num, gameDateTime);
			}
		}
	}

	private static List<Car> AllPassengerCars()
	{
		return TrainController.Shared.Cars.Where((Car car) => car.IsPassengerCar()).ToList();
	}
}
