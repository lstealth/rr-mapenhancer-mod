using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Track.Signals;

public class CTCSignalModelController : MonoBehaviour
{
	public List<SemaphoreHeadController> semaphoreHeads;

	private bool[] _activeByDefault;

	private void BuildActiveByDefaultIfNeeded()
	{
		if (_activeByDefault == null)
		{
			_activeByDefault = semaphoreHeads.Select((SemaphoreHeadController head) => head.gameObject.activeSelf).ToArray();
		}
	}

	public void Configure(int numberOfHeads)
	{
		BuildActiveByDefaultIfNeeded();
		for (int i = 0; i < semaphoreHeads.Count; i++)
		{
			bool flag = _activeByDefault[i];
			semaphoreHeads[i].gameObject.SetActive(flag && i + 1 <= numberOfHeads);
		}
	}

	public void DisplayAspect(SignalAspect aspect, string debugId)
	{
		if (semaphoreHeads.Count > 0)
		{
			SemaphoreHeadController semaphoreHeadController = semaphoreHeads[0];
			semaphoreHeadController.SetAspect(aspect switch
			{
				SignalAspect.Approach => SemaphoreHeadController.Aspect.Yellow, 
				SignalAspect.Clear => SemaphoreHeadController.Aspect.Green, 
				_ => SemaphoreHeadController.Aspect.Red, 
			});
		}
		if (semaphoreHeads.Count > 1)
		{
			SemaphoreHeadController semaphoreHeadController = semaphoreHeads[1];
			semaphoreHeadController.SetAspect(aspect switch
			{
				SignalAspect.DivergingApproach => SemaphoreHeadController.Aspect.Yellow, 
				SignalAspect.DivergingClear => SemaphoreHeadController.Aspect.Green, 
				_ => SemaphoreHeadController.Aspect.Red, 
			});
		}
		if (semaphoreHeads.Count > 2)
		{
			SemaphoreHeadController semaphoreHeadController2 = semaphoreHeads[2];
			SemaphoreHeadController.Aspect aspect2 = ((aspect == SignalAspect.Restricting) ? SemaphoreHeadController.Aspect.Yellow : SemaphoreHeadController.Aspect.Red);
			semaphoreHeadController2.SetAspect(aspect2);
		}
	}
}
