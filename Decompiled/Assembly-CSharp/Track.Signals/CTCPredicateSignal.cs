using System;
using System.Collections.Generic;
using Game.State;
using UnityEngine;

namespace Track.Signals;

public class CTCPredicateSignal : CTCSignal
{
	[Serializable]
	public class HeadPredicates
	{
		public List<Predicate> predicates;

		public CTCSignal nextSignal;
	}

	[Serializable]
	public class Predicate
	{
		public PredicateType type;

		public TrackNode switchNode;

		public SwitchSetting switchSetting;

		public List<CTCBlock> blocks;

		public CTCInterlocking interlocking;

		public SignalDirection direction;
	}

	public enum PredicateType
	{
		Switch,
		Block,
		InterlockingTrafficDirection,
		InterlockingTrafficDirectionIsNot,
		AlwaysFalse
	}

	[Tooltip("One predicate per head.")]
	public List<HeadPredicates> heads;

	protected override void OnEnable()
	{
		base.OnEnable();
		if (!StateManager.IsHost)
		{
			return;
		}
		foreach (HeadPredicates head in heads)
		{
			foreach (Predicate predicate in head.predicates)
			{
				switch (predicate.type)
				{
				case PredicateType.Switch:
					UpdateOnChange<SwitchSetting>(Storage.ObserveSwitchPosition, predicate.switchNode.id);
					break;
				case PredicateType.Block:
					foreach (CTCBlock block in predicate.blocks)
					{
						UpdateOnChange<bool>(Storage.ObserveBlockOccupancy, block.id);
					}
					break;
				case PredicateType.InterlockingTrafficDirection:
				case PredicateType.InterlockingTrafficDirectionIsNot:
					UpdateOnChange<SignalDirection>(Storage.ObserveInterlockingDirection, predicate.interlocking.id);
					if (predicate.switchNode != null)
					{
						UpdateOnChange<SwitchSetting>(Storage.ObserveSwitchPosition, predicate.switchNode.id);
					}
					break;
				}
			}
			if (head.nextSignal != null)
			{
				UpdateOnChange<SignalAspect>(Storage.ObserveSignalAspect, head.nextSignal.id);
			}
		}
	}

	private void OnValidate()
	{
		int num = headConfiguration.IntHeadCount();
		if (heads == null)
		{
			heads = new List<HeadPredicates>();
		}
		if (num != heads.Count)
		{
			while (heads.Count > num)
			{
				heads.RemoveAt(heads.Count - 1);
			}
			while (heads.Count < num)
			{
				heads.Add(new HeadPredicates());
			}
		}
	}

	protected override SignalAspect CalculateAspect(out StopReason stopReason)
	{
		SemaphoreHeadController.Aspect head = SemaphoreHeadController.Aspect.Red;
		SemaphoreHeadController.Aspect head2 = SemaphoreHeadController.Aspect.Red;
		SemaphoreHeadController.Aspect head3 = SemaphoreHeadController.Aspect.Red;
		for (int i = 0; i < heads.Count; i++)
		{
			HeadPredicates headPredicates = heads[i];
			bool num = IsSatisfied(headPredicates);
			SemaphoreHeadController.Aspect aspect = SemaphoreHeadController.Aspect.Red;
			if (num)
			{
				aspect = ((headPredicates.nextSignal == null || !headPredicates.nextSignal.isActiveAndEnabled) ? SemaphoreHeadController.Aspect.Yellow : ((AspectDisplayedBySignal(headPredicates.nextSignal) == SignalAspect.Stop) ? SemaphoreHeadController.Aspect.Yellow : SemaphoreHeadController.Aspect.Green));
			}
			if (i == 0)
			{
				head = aspect;
			}
			if (i == 1)
			{
				head2 = aspect;
			}
			if (i == 2)
			{
				head3 = aspect;
			}
		}
		stopReason = StopReason.None;
		return CTCSignal.SignalAspectForHeads(head, head2, head3);
	}

	private bool IsSatisfied(HeadPredicates headPredicates)
	{
		return headPredicates.predicates.TrueForAll(IsSatisfied);
	}

	private bool IsSatisfied(Predicate predicate)
	{
		switch (predicate.type)
		{
		case PredicateType.Switch:
			return predicate.switchNode.CTCSwitchSetting() == predicate.switchSetting;
		case PredicateType.Block:
			return predicate.blocks.TrueForAll((CTCBlock block) => !block.IsOccupied);
		case PredicateType.InterlockingTrafficDirection:
			if (base.IsCTC)
			{
				return predicate.interlocking.Direction == predicate.direction;
			}
			return true;
		case PredicateType.InterlockingTrafficDirectionIsNot:
			if (base.IsCTC)
			{
				if (predicate.switchNode != null && predicate.switchNode.CTCSwitchSetting() != predicate.switchSetting)
				{
					return true;
				}
				return predicate.interlocking.Direction != predicate.direction;
			}
			return true;
		case PredicateType.AlwaysFalse:
			return false;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}
}
