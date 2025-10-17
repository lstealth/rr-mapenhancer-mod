using System;
using System.Collections.Generic;
using System.Linq;
using Core.Diagnostics;
using Game.State;
using JetBrains.Annotations;
using Serilog;
using UnityEngine;

namespace Track.Signals;

public class CTCAutoSignal : CTCSignal
{
	[Tooltip("Blocks protected by this signal.")]
	public List<CTCBlock> blocks;

	[Tooltip("Maps each signal head to an interlocking route. One entry per head.")]
	public List<int> interlockingRouteMapping;

	protected override void OnEnable()
	{
		base.OnEnable();
		if (!StateManager.IsHost)
		{
			return;
		}
		if (base.Interlocking != null)
		{
			UpdateOnChange<SignalDirection>(Storage.ObserveInterlockingDirection, base.Interlocking.id);
			foreach (CTCInterlocking.SwitchSet switchSet in base.Interlocking.switchSets)
			{
				foreach (TrackNode switchNode in switchSet.switchNodes)
				{
					UpdateOnChange<SwitchSetting>(Storage.ObserveSwitchPosition, switchNode.id);
				}
			}
			foreach (int item in interlockingRouteMapping)
			{
				var (readOnlyCollection, cTCSignal, _) = base.Interlocking.BlockAndNexSignal(item, direction);
				if (cTCSignal != null)
				{
					UpdateOnChange<SignalAspect>(Storage.ObserveSignalAspect, cTCSignal.id);
				}
				foreach (CTCBlock item2 in readOnlyCollection)
				{
					UpdateOnChange<bool>(Storage.ObserveBlockOccupancy, item2.id);
					UpdateOnChange<CTCTrafficFilter>(Storage.ObserveBlockTrafficFilter, item2.id);
				}
			}
		}
		else if (base.Intermediate != null)
		{
			CTCSignal cTCSignal2 = base.Intermediate.NextSignal(this, CTCDirection.Left);
			CTCSignal cTCSignal3 = base.Intermediate.NextSignal(this, CTCDirection.Right);
			if (cTCSignal2 != null)
			{
				UpdateOnChange<SignalAspect>(Storage.ObserveSignalAspect, cTCSignal2.id);
			}
			if (cTCSignal3 != null)
			{
				UpdateOnChange<SignalAspect>(Storage.ObserveSignalAspect, cTCSignal3.id);
			}
		}
		foreach (CTCBlock block in blocks)
		{
			if (block == null)
			{
				Debug.LogWarning("Signal " + base.name + " has null block");
				continue;
			}
			UpdateOnChange<bool>(Storage.ObserveBlockOccupancy, block.id);
			UpdateOnChange<CTCTrafficFilter>(Storage.ObserveBlockTrafficFilter, block.id);
		}
	}

	private void OnDrawGizmosSelected()
	{
		foreach (CTCBlock block in blocks)
		{
			if (!(block == null))
			{
				block.DrawGizmos(new Color(1f, 0.4f, 0f), 2f);
			}
		}
	}

	protected override SignalAspect CalculateAspect(out StopReason stopReason)
	{
		try
		{
			return _CalculateAspect(out stopReason, null);
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Exception calculating aspect for {autoSignal}:", this);
			Debug.LogException(exception);
			stopReason = StopReason.None;
			return SignalAspect.Stop;
		}
	}

	private SignalAspect _CalculateAspect(out StopReason stopReason, IDiagnosticCollector diagnostics)
	{
		stopReason = StopReason.None;
		foreach (CTCBlock block in blocks)
		{
			if (!(block == null))
			{
				CTCTrafficFilter trafficFilter = block.TrafficFilter;
				if (!DirectionMatches(direction, trafficFilter))
				{
					diagnostics?.Log($"Signal {base.DisplayName} Stop: Block {block.id} direction {trafficFilter} is against signal direction {direction}.");
					stopReason = StopReason.OpposingDirection;
					return SignalAspect.Stop;
				}
				if (block.IsOccupied)
				{
					diagnostics?.Log("Signal " + base.DisplayName + " Stop: Block " + block.id + " is occupied.");
					stopReason = StopReason.Occupied;
					return SignalAspect.Stop;
				}
			}
		}
		SemaphoreHeadController.Aspect head = SemaphoreHeadController.Aspect.Red;
		SemaphoreHeadController.Aspect head2 = SemaphoreHeadController.Aspect.Red;
		if (base.Interlocking != null)
		{
			if (interlockingRouteMapping.Count == 0)
			{
				Log.Error("Signal {signalId} is in interlocking {interlockingId} but has empty interlockingRouteMapping -- no aspects will be displayed", id, base.Interlocking.id);
			}
			if (interlockingRouteMapping.Count >= 1)
			{
				int routeIndex = interlockingRouteMapping[0];
				(IReadOnlyCollection<CTCBlock>, CTCSignal, bool) tuple = base.Interlocking.BlockAndNexSignal(routeIndex, direction);
				IReadOnlyCollection<CTCBlock> item = tuple.Item1;
				CTCSignal item2 = tuple.Item2;
				bool item3 = tuple.Item3;
				head = AspectForBlockAndNextSignal(item, item2, item3);
			}
			if (interlockingRouteMapping.Count >= 2)
			{
				int routeIndex2 = interlockingRouteMapping[1];
				(IReadOnlyCollection<CTCBlock>, CTCSignal, bool) tuple2 = base.Interlocking.BlockAndNexSignal(routeIndex2, direction);
				IReadOnlyCollection<CTCBlock> item4 = tuple2.Item1;
				CTCSignal item5 = tuple2.Item2;
				bool item6 = tuple2.Item3;
				head2 = AspectForBlockAndNextSignal(item4, item5, item6);
			}
		}
		else if (base.Intermediate != null)
		{
			if (base.Intermediate.IsNextInterlockingSignalAgainst(this, null))
			{
				head = SemaphoreHeadController.Aspect.Red;
			}
			else
			{
				CTCSignal nextSignal = base.Intermediate.NextSignal(this, direction);
				head = AspectForBlockAndNextSignal(null, nextSignal, lined: true);
			}
		}
		else
		{
			head = SemaphoreHeadController.Aspect.Yellow;
		}
		return CTCSignal.SignalAspectForHeads(head, head2, SemaphoreHeadController.Aspect.Red);
	}

	private SemaphoreHeadController.Aspect AspectForBlockAndNextSignal([CanBeNull] IReadOnlyCollection<CTCBlock> nextBlocks, CTCSignal nextSignal, bool lined)
	{
		if (!lined)
		{
			return SemaphoreHeadController.Aspect.Red;
		}
		if (nextBlocks != null && nextBlocks.Any((CTCBlock b) => b.IsOccupied))
		{
			return SemaphoreHeadController.Aspect.Red;
		}
		if (nextSignal == null || !nextSignal.isActiveAndEnabled)
		{
			return SemaphoreHeadController.Aspect.Yellow;
		}
		return AspectDisplayedBySignal(nextSignal) switch
		{
			SignalAspect.Stop => SemaphoreHeadController.Aspect.Yellow, 
			SignalAspect.Approach => SemaphoreHeadController.Aspect.Green, 
			SignalAspect.Clear => SemaphoreHeadController.Aspect.Green, 
			SignalAspect.DivergingApproach => SemaphoreHeadController.Aspect.Yellow, 
			SignalAspect.DivergingClear => SemaphoreHeadController.Aspect.Yellow, 
			SignalAspect.Restricting => SemaphoreHeadController.Aspect.Yellow, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	private static bool DirectionMatches(CTCDirection signalDirection, CTCTrafficFilter trafficFilter)
	{
		return trafficFilter switch
		{
			CTCTrafficFilter.None => false, 
			CTCTrafficFilter.Right => signalDirection == CTCDirection.Right, 
			CTCTrafficFilter.Left => signalDirection == CTCDirection.Left, 
			CTCTrafficFilter.Any => true, 
			_ => throw new ArgumentOutOfRangeException("trafficFilter", trafficFilter, null), 
		};
	}

	private static bool DirectionMatches(SignalDirection signalDirection, CTCTrafficFilter trafficFilter)
	{
		return trafficFilter switch
		{
			CTCTrafficFilter.None => false, 
			CTCTrafficFilter.Right => signalDirection == SignalDirection.Right, 
			CTCTrafficFilter.Left => signalDirection == SignalDirection.Left, 
			CTCTrafficFilter.Any => true, 
			_ => throw new ArgumentOutOfRangeException("trafficFilter", trafficFilter, null), 
		};
	}
}
