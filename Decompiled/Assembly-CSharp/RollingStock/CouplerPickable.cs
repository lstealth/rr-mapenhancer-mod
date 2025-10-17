using System;
using UnityEngine;

namespace RollingStock;

public class CouplerPickable : MonoBehaviour, IPickable
{
	public Action activate;

	public bool isOpen { get; set; }

	public float MaxPickDistance => 75f;

	public int Priority => 0;

	public TooltipInfo TooltipInfo => new TooltipInfo("Coupler", isOpen ? null : "Click to Open");

	public PickableActivationFilter ActivationFilter => PickableActivationFilter.PrimaryOnly;

	public void Activate(PickableActivateEvent evt)
	{
		activate();
	}

	public void Deactivate()
	{
	}

	private void OnEnable()
	{
		MovingColliderScaler.Shared.Register(GetComponent<CapsuleCollider>());
	}

	private void OnDisable()
	{
		MovingColliderScaler.Shared.Unregister(GetComponent<CapsuleCollider>());
	}
}
