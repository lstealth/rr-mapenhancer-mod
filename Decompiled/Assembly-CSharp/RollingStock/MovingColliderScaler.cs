using System.Collections;
using System.Collections.Generic;
using Helpers;
using UnityEngine;

namespace RollingStock;

public class MovingColliderScaler : MonoBehaviour
{
	private struct Entry
	{
		public bool Valid;

		public readonly CapsuleCollider Collider;

		public readonly Transform Transform;

		public Vector3 LastPosition;

		public readonly float ColliderRadius0;

		public Entry(bool valid, CapsuleCollider collider, Vector3 lastPosition)
		{
			Valid = valid;
			Collider = collider;
			ColliderRadius0 = collider.radius;
			Transform = collider.transform;
			LastPosition = lastPosition;
		}
	}

	private static MovingColliderScaler _shared;

	[SerializeField]
	private float maxScale = 2f;

	[SerializeField]
	private float speedLow = 3f;

	[SerializeField]
	private float speedHigh = 10f;

	private Coroutine _coroutine;

	private readonly List<Entry> _colliders = new List<Entry>(64);

	private int _nextFreeSlot;

	public static MovingColliderScaler Shared
	{
		get
		{
			if (_shared == null)
			{
				_shared = new GameObject("MovingColliderScaler")
				{
					hideFlags = HideFlags.DontSave
				}.AddComponent<MovingColliderScaler>();
			}
			return _shared;
		}
	}

	private void OnEnable()
	{
		_coroutine = StartCoroutine(Loop());
	}

	private void OnDisable()
	{
		StopCoroutine(_coroutine);
		_coroutine = null;
	}

	public void Register(CapsuleCollider capsuleCollider)
	{
		Entry entry = new Entry(valid: true, capsuleCollider, capsuleCollider.transform.GamePosition());
		for (int i = _nextFreeSlot; i < _colliders.Count; i++)
		{
			if (!_colliders[i].Valid)
			{
				_colliders[i] = entry;
				_nextFreeSlot = i + 1;
				return;
			}
		}
		_colliders.Add(entry);
	}

	public void Unregister(CapsuleCollider theCollider)
	{
		Transform transform = theCollider.transform;
		for (int i = 0; i < _colliders.Count; i++)
		{
			Entry entry = _colliders[i];
			if (entry.Valid && !(entry.Transform != transform))
			{
				theCollider.transform.localScale = Vector3.one;
				_colliders[i] = new Entry
				{
					Valid = false
				};
				_nextFreeSlot = Mathf.Min(_nextFreeSlot, i);
				break;
			}
		}
	}

	private IEnumerator Loop()
	{
		WaitForSecondsRealtime wait = new WaitForSecondsRealtime(1f);
		float lastTime = Time.time;
		while (true)
		{
			float time = Time.time;
			float num = time - lastTime;
			if (num == 0f)
			{
				num = 1f / 60f;
			}
			for (int i = 0; i < _colliders.Count; i++)
			{
				Entry value = _colliders[i];
				if (value.Valid)
				{
					Vector3 vector = value.Transform.GamePosition();
					float value2 = Vector3.Distance(vector, value.LastPosition) / num * 2.23694f;
					value.Collider.radius = value.ColliderRadius0 * Mathf.Lerp(1f, maxScale, Mathf.InverseLerp(speedLow, speedHigh, value2));
					value.LastPosition = vector;
					_colliders[i] = value;
				}
			}
			lastTime = time;
			yield return wait;
		}
	}
}
