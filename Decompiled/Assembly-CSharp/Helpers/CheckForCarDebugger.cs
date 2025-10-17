using UnityEngine;

namespace Helpers;

public class CheckForCarDebugger : MonoBehaviour
{
	public float radius = 0.1f;

	private void OnDrawGizmos()
	{
		TrainController shared = TrainController.Shared;
		if (!(shared == null))
		{
			Gizmos.color = ((shared.CheckForCarAtPoint(WorldTransformer.WorldToGame(base.transform.position), radius) == null) ? Color.red : Color.green);
			Gizmos.DrawWireSphere(base.transform.position, radius);
		}
	}
}
