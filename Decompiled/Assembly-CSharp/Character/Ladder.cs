using Model;
using UnityEngine;

namespace Character;

[RequireComponent(typeof(CapsuleCollider))]
public class Ladder : MonoBehaviour
{
	public float height;

	private AnimationCurve movementSpeedCurve;

	private CapsuleCollider _collider;

	public CapsuleCollider CapsuleCollider => _collider;

	private void Awake()
	{
		_collider = GetComponent<CapsuleCollider>();
		OnValidate();
		movementSpeedCurve = Config.Shared.ladderMovementSpeedCurve;
	}

	private void OnValidate()
	{
		if (_collider == null)
		{
			_collider = GetComponent<CapsuleCollider>();
		}
		_collider.isTrigger = true;
		_collider.height = height;
		_collider.radius = 0.35f;
	}

	public Vector3 ClosestPointTo(Vector3 characterPosition)
	{
		Transform transform = base.transform;
		Vector3 vector = transform.position + transform.up * height / 2f;
		Vector3 vector2 = transform.position - transform.up * height / 2f;
		Vector3 vector3 = vector - vector2;
		float num = Vector3.Dot(characterPosition - vector2, vector3.normalized);
		if (num > 0f)
		{
			if (num <= vector3.magnitude)
			{
				return vector2 + vector3.normalized * num;
			}
			return vector;
		}
		return vector2;
	}

	public bool CheckPositionValid(Vector3 localPosition, bool ascending)
	{
		float num = height / 2f;
		if (ascending)
		{
			return localPosition.y <= num;
		}
		return localPosition.y > 0f - num;
	}

	public float SpeedMultiplierForPosition(Vector3 localPosition)
	{
		if (height < 1f)
		{
			return movementSpeedCurve.Evaluate(0.5f);
		}
		float y = localPosition.y;
		float num = height / 2f;
		float time = Mathf.InverseLerp(0f - num, num, y);
		return movementSpeedCurve.Evaluate(time);
	}
}
