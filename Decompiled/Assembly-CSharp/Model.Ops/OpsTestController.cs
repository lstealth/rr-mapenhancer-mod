using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Messages;
using Game.State;
using Track;
using UnityEngine;

namespace Model.Ops;

public class OpsTestController : MonoBehaviour
{
	private TrainController TrainController => TrainController.Shared;

	private Interchange Interchange => UnityEngine.Object.FindObjectsOfType<Interchange>().FirstOrDefault();

	private static OpsController OpsController => OpsController.Shared;

	private void OnGUI()
	{
		using (new GUILayout.VerticalScope())
		{
			if (GUILayout.Button("Run 1"))
			{
				StartCoroutine(Run1());
			}
		}
	}

	private TrackSpan TrackSpanForId(string trackSpanId)
	{
		return TrainController.graph.SpanForId(trackSpanId);
	}

	private Industry IndustryForId(string industryId)
	{
		return UnityEngine.Object.FindObjectsOfType<Industry>().FirstOrDefault((Industry ind) => ind.identifier == industryId);
	}

	private IEnumerator Wait(float hours)
	{
		StateManager.ApplyLocal(new WaitTime
		{
			Hours = hours
		});
		yield return new WaitForFixedUpdate();
	}

	private IEnumerator SetTime(float hours)
	{
		StateManager.ApplyLocal(new SetTimeOfDay(hours * 60f * 60f));
		yield return new WaitForFixedUpdate();
	}

	private IEnumerator MoveCarsToDestination(List<Car> cars)
	{
		foreach (Car car in cars)
		{
			OpsController.Sweep(car);
		}
		yield return new WaitForFixedUpdate();
	}

	private IEnumerator Run1()
	{
		yield return SetTime(8f);
		Interchange interchange = Interchange;
		AssertEqual(CarsOn(interchange).Count, 0);
		Industry ind1 = IndustryForId("ind1");
		ind1.SetContract(new Contract(1));
		yield return Wait(0.5f);
		List<Car> list = CarsOn(interchange);
		AssertEqual(list.Count, 1);
		Waybill? waybill = list[0].GetWaybill(OpsController);
		AssertEqual(waybill.Value.Destination.Identifier, "ind1.r1");
		AssertEqual(waybill.Value.Completed, false);
		yield return MoveCarsToDestination(list);
		AssertEqual(CarsOn(interchange).Count, 0);
		List<Car> cars1 = CarsOn(ind1);
		AssertEqual(cars1.Count, 1);
		yield return Wait(0.1f);
		Waybill? waybill2 = cars1[0].GetWaybill(OpsController);
		AssertEqual(waybill2.Value.Destination.Identifier, "ind1.r1");
		AssertEqual(waybill2.Value.Completed, true);
	}

	private List<Car> CarsOn(Industry ind)
	{
		return ind.Components.SelectMany(CarsOn).ToList();
	}

	private List<Car> CarsOn(IndustryComponent ic)
	{
		return ic.trackSpans.SelectMany(TrainController.CarsOnSpan).ToList();
	}

	private void Assert(bool condition, string message)
	{
		if (!condition)
		{
			throw new Exception("Assertion failure: " + message);
		}
	}

	private static void AssertEqual(object actual, object expected, string message = "")
	{
		if (!actual.Equals(expected))
		{
			throw new Exception($"{actual} != {expected} expected: {message}");
		}
	}
}
