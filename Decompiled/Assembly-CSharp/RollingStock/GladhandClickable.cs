using System;
using UnityEngine;

namespace RollingStock;

public class GladhandClickable : MonoBehaviour, IPickable
{
	private Anglecock _anglecock;

	public float MaxPickDistance => 30f;

	public int Priority => 1;

	public TooltipInfo TooltipInfo => new TooltipInfo("Gladhand", Anglecock.GladhandClickConnects() switch
	{
		Anglecock.GladhandClickAction.None => "", 
		Anglecock.GladhandClickAction.Connect => "Click to Connect Gladhands", 
		Anglecock.GladhandClickAction.Disconnect => "Click to Disconnect Gladhands", 
		_ => throw new ArgumentOutOfRangeException(), 
	});

	public PickableActivationFilter ActivationFilter => PickableActivationFilter.PrimaryOnly;

	private Anglecock Anglecock
	{
		get
		{
			if (_anglecock == null)
			{
				_anglecock = GetComponentInParent<Anglecock>();
			}
			return _anglecock;
		}
	}

	public void Activate(PickableActivateEvent evt)
	{
		Anglecock.GladhandClick();
	}

	public void Deactivate()
	{
	}
}
