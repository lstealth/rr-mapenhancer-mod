using UnityEngine;

namespace RollingStock;

public class CarLoadTarget : MonoBehaviour
{
	public int slotIndex;

	public float radius;

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireSphere(base.transform.position, radius);
	}
}
