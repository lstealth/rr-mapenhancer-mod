using Helpers;
using UnityEngine;

namespace RollingStock;

[RequireComponent(typeof(SphereCollider))]
public class Seat : MonoBehaviour
{
	[Tooltip("Order in which seats are cycled through. 0 is before 1.")]
	public int priority;

	private float _seatToFeet = 0.47f;

	public Vector3 FootPosition => base.transform.position - _seatToFeet * Vector3.up;

	private void Start()
	{
		if (Physics.Raycast(base.transform.position, Vector3.down, out var hitInfo, 1f, 1 << Layers.Default))
		{
			float num = base.transform.position.y - hitInfo.point.y;
			if (num < _seatToFeet)
			{
				_seatToFeet = num;
				Debug.Log($"Seat adjusting _seatToFeet: {_seatToFeet}");
			}
		}
	}
}
