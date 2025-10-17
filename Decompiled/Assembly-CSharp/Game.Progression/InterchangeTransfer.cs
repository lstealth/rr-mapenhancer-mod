using Model.Ops;
using UnityEngine;

namespace Game.Progression;

public class InterchangeTransfer : MonoBehaviour
{
	[SerializeField]
	private Interchange from;

	[SerializeField]
	private Interchange to;

	public void Apply()
	{
		OpsController.Shared.RewriteWaybills(from.Industry.identifier, to.Industry.identifier);
	}
}
