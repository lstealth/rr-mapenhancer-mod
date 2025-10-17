using System;
using System.Collections.Generic;
using System.Linq;
using Core.Diagnostics;
using UnityEngine;

namespace Track.Signals;

public class CTCIntermediate : MonoBehaviour
{
	[Tooltip("Blocks, left to right.")]
	public List<CTCBlock> blocks;

	[Tooltip("Signals, left to right. Should be one less than blocks.")]
	public List<CTCSignal> signals;

	public CTCSignal nextSignalLeft;

	public CTCSignal nextSignalRight;

	public CTCBlock GetAdjacentTo(CTCBlock block, CTCDirection direction)
	{
		int num = blocks.IndexOf(block);
		if (num < 0)
		{
			return null;
		}
		int num2 = num;
		int num3 = num2 + direction switch
		{
			CTCDirection.Right => 1, 
			CTCDirection.Left => -1, 
			_ => throw new ArgumentOutOfRangeException("direction", direction, null), 
		};
		if (num3 < 0 || num3 >= blocks.Count)
		{
			return null;
		}
		return blocks[num3];
	}

	public CTCSignal NextSignal(CTCSignal from, CTCDirection direction)
	{
		int num = signals.IndexOf(from);
		if (num < 0)
		{
			return null;
		}
		int num2 = IncrementForDirection(direction);
		for (num += num2; num >= 0 && num < signals.Count; num += num2)
		{
			if (signals[num].direction == direction)
			{
				return signals[num];
			}
		}
		return NextExternalSignalForDirection(direction);
	}

	public CTCSignal NextExternalSignalForDirection(CTCDirection direction)
	{
		return direction switch
		{
			CTCDirection.Left => nextSignalLeft, 
			CTCDirection.Right => nextSignalRight, 
			_ => throw new ArgumentOutOfRangeException("direction", direction, null), 
		};
	}

	public bool IsNextInterlockingSignalAgainst(CTCSignal from, IDiagnosticCollector diagnostics)
	{
		CTCSignal cTCSignal = NextExternalSignalForDirection(from.direction);
		if (cTCSignal == null)
		{
			return false;
		}
		CTCInterlocking interlocking = cTCSignal.Interlocking;
		if (interlocking == null)
		{
			return false;
		}
		CTCTrafficFilter trafficFilter = from.direction switch
		{
			CTCDirection.Left => CTCTrafficFilter.Left, 
			CTCDirection.Right => CTCTrafficFilter.Right, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
		return interlocking.IsTrafficAgainst(BlockAtEnd(from.direction), trafficFilter, diagnostics);
	}

	private static int IncrementForDirection(CTCDirection direction)
	{
		return direction switch
		{
			CTCDirection.Right => 1, 
			CTCDirection.Left => -1, 
			_ => throw new ArgumentOutOfRangeException("direction", direction, null), 
		};
	}

	public CTCBlock BlockAtEnd(CTCDirection direction)
	{
		return direction switch
		{
			CTCDirection.Left => blocks.FirstOrDefault(), 
			CTCDirection.Right => blocks.LastOrDefault(), 
			_ => throw new ArgumentOutOfRangeException("direction", direction, null), 
		};
	}

	public void BlockBecameUnoccupied(CTCBlock block)
	{
		if (block.TrafficFilter == CTCTrafficFilter.Any)
		{
			return;
		}
		int num = blocks.IndexOf(block);
		for (int num2 = num - 1; num2 >= 0; num2--)
		{
			CTCBlock cTCBlock = blocks[num2];
			if (cTCBlock.TrafficFilter == CTCTrafficFilter.Left)
			{
				cTCBlock.TrafficFilter = CTCTrafficFilter.None;
			}
			if (cTCBlock.IsOccupied)
			{
				break;
			}
		}
		for (int i = num + 1; i < blocks.Count; i++)
		{
			CTCBlock cTCBlock2 = blocks[i];
			if (cTCBlock2.TrafficFilter == CTCTrafficFilter.Right)
			{
				cTCBlock2.TrafficFilter = CTCTrafficFilter.None;
			}
			if (cTCBlock2.IsOccupied)
			{
				break;
			}
		}
	}
}
