using System;
using System.Collections;
using System.Collections.Generic;
using Game.State;
using Map.Runtime;
using UnityEngine;

namespace Track.Signals;

public abstract class CTCSignal : MonoBehaviour
{
	protected enum StopReason
	{
		None,
		OpposingDirection,
		NoRoute,
		Occupied
	}

	public string id;

	public SignalHeadConfiguration headConfiguration;

	[Header("Behavior")]
	[Tooltip("Direction of travel that this signal protects. Trains moving in this direction obey this signal.")]
	public CTCDirection direction;

	public CTCSignalModelController modelController;

	protected SignalStorage Storage;

	protected readonly HashSet<IDisposable> Observers = new HashSet<IDisposable>();

	private Coroutine _scheduledUpdate;

	internal SignalAspect LastShownAspect { get; private set; }

	protected bool IsCTC => Storage.SystemMode == SystemMode.CTC;

	public CTCIntermediate Intermediate { get; protected set; }

	public CTCInterlocking Interlocking { get; protected set; }

	public bool IsIntermediate => Interlocking == null;

	public string DisplayName
	{
		get
		{
			CTCInterlocking interlocking = Interlocking;
			if (interlocking == null)
			{
				return "Intermediate";
			}
			return interlocking.displayName;
		}
	}

	public SignalAspect CurrentAspect => AspectDisplayedBySignal(this);

	private void Awake()
	{
		Intermediate = GetComponentInParent<CTCIntermediate>();
		Interlocking = GetComponentInParent<CTCInterlocking>();
	}

	protected virtual void OnEnable()
	{
		Storage = GetComponentInParent<SignalStorage>();
		if (StateManager.IsHost)
		{
			Observers.Add(Storage.ObserveSystemMode(delegate
			{
				SetNeedsUpdate();
			}));
		}
		if (modelController != null)
		{
			modelController.Configure(headConfiguration.IntHeadCount());
			Observers.Add(Storage.ObserveSignalAspect(id, delegate(SignalAspect aspectValue)
			{
				modelController.DisplayAspect(aspectValue, id);
			}));
		}
	}

	private void OnDisable()
	{
		foreach (IDisposable observer in Observers)
		{
			observer.Dispose();
		}
		Observers.Clear();
	}

	private void OnDrawGizmos()
	{
		SignalAspect signalAspect = AspectDisplayedBySignal(this);
		Vector3 position = base.transform.position;
		Gizmos.color = Color.cyan;
		GizmoHelpers.DrawCircle(position, 3.6576f);
		Gizmos.color = Color.cyan * 0.7f;
		GizmoHelpers.DrawCircle(position, 2.7432f);
		Gizmos.color = signalAspect switch
		{
			SignalAspect.Stop => Color.red, 
			SignalAspect.Clear => Color.green, 
			SignalAspect.Approach => Color.yellow, 
			_ => Color.red, 
		};
		Gizmos.DrawSphere(position + Vector3.up * 3f, 0.5f);
		if (headConfiguration == SignalHeadConfiguration.Double)
		{
			Gizmos.color = signalAspect switch
			{
				SignalAspect.Stop => Color.red, 
				SignalAspect.DivergingClear => Color.green, 
				SignalAspect.DivergingApproach => Color.yellow, 
				_ => Color.red, 
			};
			Gizmos.DrawSphere(position + Vector3.up * 2f, 0.5f);
		}
	}

	protected void UpdateOnChange<T>(Func<string, Action<T>, IDisposable> observeAction, string itemId)
	{
		Observers.Add(observeAction(itemId, delegate
		{
			SetNeedsUpdate();
		}));
	}

	protected void SetNeedsUpdate()
	{
		if (StateManager.IsHost)
		{
			if (_scheduledUpdate != null)
			{
				StopCoroutine(_scheduledUpdate);
			}
			_scheduledUpdate = StartCoroutine(PerformUpdate());
		}
	}

	private IEnumerator PerformUpdate()
	{
		yield return null;
		_scheduledUpdate = null;
		StopReason stopReason;
		SignalAspect aspect = CalculateAspect(out stopReason);
		ShowAspect(aspect);
	}

	protected abstract SignalAspect CalculateAspect(out StopReason stopReason);

	protected SignalAspect AspectDisplayedBySignal(CTCSignal signal)
	{
		if (Storage == null)
		{
			return SignalAspect.Stop;
		}
		return Storage.GetSignalAspect(signal.id);
	}

	private void ShowAspect(SignalAspect aspect)
	{
		StateManager.DebugAssertIsHost();
		if (aspect != LastShownAspect)
		{
			_ = LastShownAspect;
			LastShownAspect = aspect;
		}
		Storage.SetSignalAspect(id, aspect);
	}

	protected static SignalAspect SignalAspectForHeads(SemaphoreHeadController.Aspect head0, SemaphoreHeadController.Aspect head1, SemaphoreHeadController.Aspect head2)
	{
		switch (head0)
		{
		case SemaphoreHeadController.Aspect.Green:
			return SignalAspect.Clear;
		case SemaphoreHeadController.Aspect.Yellow:
			return SignalAspect.Approach;
		default:
			switch (head1)
			{
			case SemaphoreHeadController.Aspect.Green:
				return SignalAspect.DivergingClear;
			case SemaphoreHeadController.Aspect.Yellow:
				return SignalAspect.DivergingApproach;
			default:
				if (head2 != SemaphoreHeadController.Aspect.Red)
				{
					return SignalAspect.Restricting;
				}
				return SignalAspect.Stop;
			}
		}
	}
}
