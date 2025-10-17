using UnityEngine;

namespace RollingStock;

public class PrefabRuntimeReparent : MonoBehaviour
{
	[Tooltip("Transform to become the new parent of this object in Awake().")]
	public Transform target;

	private void Awake()
	{
		base.transform.SetParent(target, worldPositionStays: true);
	}
}
