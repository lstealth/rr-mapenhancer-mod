using System;
using Core;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using JetBrains.Annotations;
using UnityEngine;

namespace Track;

[ExecuteInEditMode]
public class TrackNode : MonoBehaviour
{
	public string id;

	public bool flipSwitchStand;

	[CanBeNull]
	public Turntable turntable;

	public Action OnDidChangeThrown;

	private bool _isThrown;

	public bool isThrown
	{
		get
		{
			return _isThrown;
		}
		set
		{
			if (value != _isThrown)
			{
				_isThrown = value;
				if (IsCTCSwitchUnlocked)
				{
					CTCDisplayThrown = !CTCDisplayThrown;
				}
				OnDidChangeThrown?.Invoke();
				Messenger.Default.Send(new SwitchThrownDidChange(this));
			}
		}
	}

	public bool IsCTCSwitch { get; set; }

	public bool IsCTCSwitchUnlocked { get; set; }

	public bool CTCDisplayThrown { get; private set; }

	private void Awake()
	{
		if (!string.IsNullOrEmpty(id))
		{
			IdGenerator.TrackNodes.Add(id);
		}
	}

	private void OnDrawGizmosSelected()
	{
		Vector3 center = base.transform.position;
		Gizmos.color = Color.yellow;
		DrawParallelLines(base.transform, 4.8768f);
		void DrawParallelLines(Transform xform, float radius)
		{
			Gizmos.DrawLine(center + xform.right * radius + xform.forward * 2f, center + xform.right * radius - xform.forward * 2f);
			Gizmos.DrawLine(center - xform.right * radius + xform.forward * 2f, center - xform.right * radius - xform.forward * 2f);
		}
	}

	public override string ToString()
	{
		return base.name + " id=" + id;
	}

	public Vector3 TangentPointAlongSegment(TrackSegment segment, float d)
	{
		TrackNode trackNode = (((object)segment.a == this) ? segment.b : segment.a);
		Vector3 vector = Transform(Vector3.forward);
		Vector3 vector2 = Transform(Vector3.back);
		float magnitude = (vector - trackNode.transform.localPosition).magnitude;
		float magnitude2 = (vector2 - trackNode.transform.localPosition).magnitude;
		float num = ((magnitude < magnitude2) ? d : (0f - d));
		return Transform(Vector3.forward * num);
	}

	public bool SegmentCanReachSegment(TrackSegment a, TrackSegment b)
	{
		Vector3 vector = TangentPointAlongSegment(a, 1f);
		Vector3 vector2 = TangentPointAlongSegment(b, 1f);
		return Vector3.SqrMagnitude(vector - vector2) > 0.1f;
	}

	private Vector3 Transform(Vector3 v)
	{
		return base.transform.localRotation * v + base.transform.localPosition;
	}
}
