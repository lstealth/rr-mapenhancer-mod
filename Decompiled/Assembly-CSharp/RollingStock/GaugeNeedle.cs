using UnityEngine;

namespace RollingStock;

public class GaugeNeedle : GaugeBehaviour
{
	[Tooltip("Minimum value displayable by the gauge.")]
	public float minValue;

	[Tooltip("Maximum value displayable by the gauge.")]
	public float maxValue = 200f;

	[Tooltip("Degrees from twelve o'clock that correspond to minValue.")]
	public float minAngle;

	[Tooltip("Degrees from twelve o'clock that correspond to maxValue.")]
	public float maxAngle = 270f;

	[Tooltip("Needle that will be rotated on its Z axis, in addition to its initial rotation. Needle should be oriented at twelve o'clock and centered on its rotation point.")]
	public Transform needle;

	private Quaternion _initialRotation;

	private Coroutine _coroutine;

	private float _displayValue;

	private void Awake()
	{
		_initialRotation = needle?.localRotation ?? Quaternion.identity;
	}

	private void OnDrawGizmosSelected()
	{
		if (!(needle == null))
		{
			Gizmos.matrix = base.transform.localToWorldMatrix;
			Gizmos.color = Color.red;
			Gizmos.DrawLine(Vector3.zero, PointForAngle(minAngle));
			Gizmos.color = Color.green;
			Gizmos.DrawLine(Vector3.zero, PointForAngle(maxAngle));
		}
		static Vector3 PointForAngle(float angle)
		{
			return Quaternion.Euler(0f, 0f, angle) * Vector3.up * 0.1f;
		}
	}

	private void OnEnable()
	{
		ValueDidChange();
	}

	protected override void ValueDidChange()
	{
		float z = Mathf.Lerp(minAngle, maxAngle, Mathf.InverseLerp(minValue, maxValue, base.Value));
		Quaternion quaternion = Quaternion.Euler(0f, 0f, z);
		needle.localRotation = quaternion * _initialRotation;
	}
}
