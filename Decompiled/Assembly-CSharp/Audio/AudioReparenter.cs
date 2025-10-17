using System;
using Helpers;
using UnityEngine;

namespace Audio;

public class AudioReparenter : MonoBehaviour
{
	private Rigidbody _rigidbody;

	[NonSerialized]
	public Transform BodyTransform;

	public Rigidbody Rigidbody
	{
		get
		{
			if (_rigidbody == null)
			{
				SetupRigidbody();
			}
			return _rigidbody;
		}
	}

	public Transform Reparent(Transform originalParent, out Vector3 offset)
	{
		offset = BodyTransform.InverseTransformPoint(originalParent.position);
		return Rigidbody.transform;
	}

	private void SetupRigidbody()
	{
		GameObject gameObject = new GameObject("AudioReparent");
		gameObject.transform.SetParent(base.transform, worldPositionStays: false);
		_rigidbody = gameObject.AddKinematicRigidbody();
	}
}
