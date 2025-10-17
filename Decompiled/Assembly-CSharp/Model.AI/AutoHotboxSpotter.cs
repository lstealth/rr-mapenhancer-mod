using System.Collections;
using System.Collections.Generic;
using Core;
using RollingStock;
using Serilog;
using UnityEngine;

namespace Model.AI;

public class AutoHotboxSpotter : AutoEngineerComponentBase
{
	private Coroutine _spotterCoroutine;

	private Coroutine _removerCoroutine;

	private IReadOnlyList<Car> _cars = new List<Car>();

	private readonly HashSet<Car> _knownHotboxes = new HashSet<Car>();

	public bool HotboxSpotted { get; private set; }

	private bool HasCars
	{
		get
		{
			if (_cars != null)
			{
				return _cars.Count > 0;
			}
			return false;
		}
	}

	private void OnEnable()
	{
		_spotterCoroutine = StartCoroutine(SpotterLoop());
		_removerCoroutine = StartCoroutine(RemoverLoop());
		UpdateHotboxRestriction();
	}

	private void OnDisable()
	{
		_ = _locomotive != null;
		StopCoroutine(_spotterCoroutine);
		_spotterCoroutine = null;
		StopCoroutine(_removerCoroutine);
		_removerCoroutine = null;
	}

	private IEnumerator SpotterLoop()
	{
		while (true)
		{
			if (!HasCars)
			{
				yield return new WaitForSeconds(1f);
				continue;
			}
			CheckForHotbox();
			while (HasCars)
			{
				int num = Random.Range(60, 300);
				yield return new WaitForSeconds(num);
				CheckForHotbox();
			}
		}
	}

	private void CheckForHotbox()
	{
		int num = -1;
		for (int i = 0; i < _cars.Count; i++)
		{
			if (_locomotive == _cars[i])
			{
				num = i;
				break;
			}
		}
		if (num < 0)
		{
			Log.Warning("Couldn't find engine in coupled cars");
			return;
		}
		int count = _knownHotboxes.Count;
		for (int num2 = num; num2 >= 0; num2--)
		{
			Car car = _cars[num2];
			Check(car);
		}
		for (int j = num; j < _cars.Count; j++)
		{
			Car car2 = _cars[j];
			Check(car2);
		}
		int count2 = _knownHotboxes.Count;
		if (count2 > count)
		{
			int number = count2 - count;
			Say(number.Pluralize("hotbox") + " spotted!");
		}
		UpdateHotboxRestriction();
		bool Check(Car car3)
		{
			if (car3 == null)
			{
				return false;
			}
			if (!car3.HasHotbox)
			{
				return false;
			}
			return _knownHotboxes.Add(car3);
		}
	}

	private IEnumerator RemoverLoop()
	{
		while (true)
		{
			yield return new WaitForSeconds(5f);
			bool flag = true;
			foreach (Car knownHotbox in _knownHotboxes)
			{
				if (knownHotbox.HasHotbox)
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				_knownHotboxes.Clear();
				UpdateHotboxRestriction();
			}
		}
	}

	private void UpdateHotboxRestriction()
	{
		bool hotboxSpotted = _knownHotboxes.Count > 0;
		HotboxSpotted = hotboxSpotted;
	}

	public void UpdateCars(IReadOnlyList<Car> cars)
	{
		_cars = cars;
		_knownHotboxes.IntersectWith(_cars);
	}

	public override void ApplyMovement(MovementInfo info)
	{
	}

	public override void WillMove()
	{
	}
}
