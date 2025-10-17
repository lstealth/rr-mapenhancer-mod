using System;
using System.Collections.Generic;
using System.Linq;
using Model;
using Model.Physics;
using UnityEngine;

namespace UI.Console.Commands;

[ConsoleCommand("/airtest", null)]
public class AirTestCommand : IConsoleCommand
{
	public string Execute(string[] comps)
	{
		BaseLocomotive selectedLocomotive = TrainController.Shared.SelectedLocomotive;
		if (selectedLocomotive == null)
		{
			return "No selected locomotive";
		}
		List<Car> list = selectedLocomotive.set.EnumerateAirOpenTo(selectedLocomotive).ToList();
		int index = ((list.IndexOf(selectedLocomotive) == 0) ? (list.Count - 1) : 0);
		selectedLocomotive.locomotiveControl.TrainBrakeSetting = 13f / 45f;
		float distance = Vector3.Distance(selectedLocomotive.BodyTransform.position, list[index].BodyTransform.position);
		CarAirSystem air = list[index].air;
		AwaitChange(Time.time, air.BrakeLine.Pressure, distance, air);
		return null;
	}

	private void AwaitChange(float t0, float brakeLine0, float distance, CarAirSystem air)
	{
		if (Mathf.Abs(air.BrakeLine.Pressure - brakeLine0) > 1f)
		{
			float num = Time.time - t0;
			Debug.Log($"{num:F3} s, {distance * 3.28084f / num:F3} ft/s");
		}
		else
		{
			LeanTween.delayedCall(0.1f, (Action)delegate
			{
				AwaitChange(t0, brakeLine0, distance, air);
			});
		}
	}
}
