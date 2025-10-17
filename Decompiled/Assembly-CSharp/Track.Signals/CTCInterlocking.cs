using System;
using System.Collections.Generic;
using System.Linq;
using Core.Diagnostics;
using Game.Messages;
using Game.State;
using Serilog;
using UnityEngine;
using UnityEngine.Serialization;

namespace Track.Signals;

public class CTCInterlocking : MonoBehaviour
{
	[Serializable]
	public struct SwitchSet
	{
		public List<TrackNode> switchNodes;
	}

	[Serializable]
	public struct Route
	{
		[FormerlySerializedAs("switchSettings")]
		public List<SwitchFilter> switchFilters;

		public int outletLeft;

		public int outletRight;
	}

	[Serializable]
	public struct Outlet
	{
		[Tooltip("Direction of this outlet relative to the interlocking. Direction of travel _from_ the interlocking.")]
		public CTCDirection direction;

		[Tooltip("Blocks that this outlet connects directly to.")]
		public List<CTCBlock> blocks;

		[Tooltip("Signal beyond 'block'.")]
		public CTCSignal nextSignal;

		public IReadOnlyCollection<CTCBlock> Blocks
		{
			get
			{
				List<CTCBlock> list = blocks;
				if (list != null && list.Count > 0)
				{
					return blocks;
				}
				return (IReadOnlyCollection<CTCBlock>)(object)Array.Empty<CTCBlock>();
			}
		}
	}

	public enum CodeFailureReason
	{
		None,
		BlockOccupied,
		NoRoute,
		RouteSetAgainst
	}

	public string id;

	public string displayName;

	public List<SwitchSet> switchSets;

	public List<Outlet> outlets;

	public List<Route> routes;

	private IReadOnlyList<CTCBlock> _blocks;

	private SignalStorage _storage;

	private IDisposable _directionObserver;

	private readonly HashSet<IDisposable> _routeBlockObservers = new HashSet<IDisposable>();

	public IReadOnlyList<CTCBlock> Blocks
	{
		get
		{
			if (_blocks == null)
			{
				_blocks = GetComponentsInChildren<CTCBlock>(includeInactive: true);
			}
			return _blocks;
		}
	}

	public SignalDirection Direction
	{
		get
		{
			return _storage.GetInterlockingDirection(id);
		}
		set
		{
			Log.Debug("Interlocking {interlockingId}: Direction has changed to {direction}", id, value);
			_storage.SetInterlockingDirection(id, value);
		}
	}

	private void OnEnable()
	{
		_storage = GetComponentInParent<SignalStorage>();
		if (StateManager.IsHost)
		{
			_directionObserver = _storage.ObserveInterlockingDirection(id, delegate
			{
				UpdateObservedRouteBlocks();
			});
		}
	}

	private void OnDisable()
	{
		_directionObserver?.Dispose();
		StopObservingRouteBlocks();
	}

	private void ObserveRouteBlock(CTCBlock block)
	{
		_routeBlockObservers.Add(_storage.ObserveBlockOccupancy(block.id, delegate(bool occupied)
		{
			if (occupied)
			{
				CodeDirection(SignalDirection.None, out var _);
			}
		}));
	}

	private void StopObservingRouteBlocks()
	{
		foreach (IDisposable routeBlockObserver in _routeBlockObservers)
		{
			routeBlockObserver.Dispose();
		}
		_routeBlockObservers.Clear();
	}

	private void UpdateObservedRouteBlocks()
	{
		StopObservingRouteBlocks();
		SignalDirection direction = Direction;
		if (direction == SignalDirection.None)
		{
			return;
		}
		Route? route = RouteForCurrentSwitchSettings();
		if (!route.HasValue)
		{
			return;
		}
		Route value = route.Value;
		HashSet<CTCBlock> hashSet = new HashSet<CTCBlock>();
		Outlet? outlet = OutletForDirection(value, direction);
		if (outlet.HasValue)
		{
			IReadOnlyCollection<CTCBlock> blocks = outlet.Value.Blocks;
			hashSet.UnionWith(blocks);
		}
		hashSet.UnionWith(Blocks);
		foreach (CTCBlock item in hashSet)
		{
			ObserveRouteBlock(item);
		}
	}

	public bool Code(SignalDirection direction, List<(TrackNode, SwitchSetting)> switchSettings, out CodeFailureReason reason)
	{
		StateManager.DebugAssertIsHost();
		Log.Debug("Code {interlockingId} {direction}", id, direction);
		Log.Debug("Code {interlockingId} {direction}: Resetting", id, direction);
		if (!CodeDirection(SignalDirection.None, out reason))
		{
			Log.Warning("Code {interlockingId} {direction}: Reset failed: {reason}", id, direction, reason);
			return false;
		}
		if (!CodeSwitchChanges(switchSettings, out reason))
		{
			Log.Warning("Code {interlockingId} {direction}: Code switch changes failed: {reason}", id, direction, reason);
			return false;
		}
		if (direction != SignalDirection.None)
		{
			bool num = CodeDirection(direction, out reason);
			if (!num)
			{
				Log.Warning("Code {interlockingId} {direction}: Code direction failed: {reason}", id, direction, reason);
			}
			return num;
		}
		return true;
	}

	private bool CodeDirection(SignalDirection direction, out CodeFailureReason reason)
	{
		StateManager.DebugAssertIsHost();
		Route? route = RouteForCurrentSwitchSettings();
		if (!route.HasValue)
		{
			reason = CodeFailureReason.NoRoute;
			return false;
		}
		Route value = route.Value;
		CTCTrafficFilter trafficFilter = direction.AsFilter();
		bool flag = direction == SignalDirection.None;
		Outlet? outlet = OutletForDirection(value, flag ? Direction : direction);
		if (outlet.HasValue)
		{
			Outlet value2 = outlet.Value;
			IReadOnlyCollection<CTCBlock> blocks = value2.Blocks;
			StringDiagnosticCollector stringDiagnosticCollector = new StringDiagnosticCollector();
			if (!SetTrafficFilterFrom(blocks, value2.direction, trafficFilter, flag, stringDiagnosticCollector))
			{
				Log.Warning("Failed to code direction {direction}: {log}", direction, stringDiagnosticCollector);
				reason = CodeFailureReason.RouteSetAgainst;
				return false;
			}
		}
		foreach (CTCBlock block in Blocks)
		{
			block.TrafficFilter = trafficFilter;
		}
		Direction = direction;
		reason = CodeFailureReason.None;
		return true;
	}

	private Outlet? OutletForDirection(Route route, SignalDirection direction)
	{
		return direction switch
		{
			SignalDirection.None => null, 
			SignalDirection.Right => outlets[route.outletRight], 
			SignalDirection.Left => outlets[route.outletLeft], 
			_ => throw new ArgumentOutOfRangeException("direction", direction, null), 
		};
	}

	private Outlet OutletForDirection(Route route, CTCDirection direction)
	{
		return direction switch
		{
			CTCDirection.Right => outlets[route.outletRight], 
			CTCDirection.Left => outlets[route.outletLeft], 
			_ => throw new ArgumentOutOfRangeException("direction", direction, null), 
		};
	}

	private Route? RouteForCurrentSwitchSettings()
	{
		foreach (Route route in routes)
		{
			if (IsLined(route))
			{
				return route;
			}
		}
		return null;
	}

	private bool SetTrafficFilterFrom(IReadOnlyCollection<CTCBlock> blocks, CTCDirection propagateDirection, CTCTrafficFilter trafficFilter, bool isResetting, IDiagnosticCollector diagnostics)
	{
		Log.Information("Interlocking {interlockingId}: SetDirectionFrom: blocks = {blocks}, propagateDirection = {propagateDirection}, trafficFilter = {trafficFilter}, isResetting = {isResetting}", id, blocks, propagateDirection, trafficFilter, isResetting);
		if (IsNextInterlockingTrafficAgainst(blocks, propagateDirection, trafficFilter, diagnostics))
		{
			return false;
		}
		bool checkOccupied = !isResetting;
		if (!SetTrafficFilterFromHelper(blocks, propagateDirection, trafficFilter, checkOccupied, dryRun: true, diagnostics))
		{
			return false;
		}
		return SetTrafficFilterFromHelper(blocks, propagateDirection, trafficFilter, checkOccupied, dryRun: false, diagnostics);
	}

	private bool SetTrafficFilterFromHelper(IReadOnlyCollection<CTCBlock> blocks, CTCDirection propagateDirection, CTCTrafficFilter trafficFilter, bool checkOccupied, bool dryRun, IDiagnosticCollector diagnostics)
	{
		if (!blocks.Any())
		{
			return true;
		}
		if (checkOccupied)
		{
			foreach (CTCBlock block in blocks)
			{
				if (block.IsOccupied)
				{
					diagnostics?.Log("Block " + block.id + " is occupied.");
					return false;
				}
			}
		}
		foreach (CTCBlock block2 in blocks)
		{
			if (block2.isActiveAndEnabled && !block2.TrySetDirection(propagateDirection, trafficFilter, dryRun))
			{
				diagnostics?.Log($"Block {block2.id} conflicts with {propagateDirection} {trafficFilter}.");
				return false;
			}
		}
		return true;
	}

	private bool IsNextInterlockingTrafficAgainst(IEnumerable<CTCBlock> blocks, CTCDirection direction, CTCTrafficFilter trafficFilter, IDiagnosticCollector diagnostics)
	{
		foreach (CTCBlock block in blocks)
		{
			CTCIntermediate intermediate = block.Intermediate;
			if (!(intermediate == null))
			{
				CTCInterlocking interlocking = intermediate.NextExternalSignalForDirection(direction).Interlocking;
				if (!(interlocking == null) && interlocking.IsTrafficAgainst(intermediate.BlockAtEnd(direction), trafficFilter, diagnostics))
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool IsTrafficAgainst(CTCBlock block, CTCTrafficFilter trafficFilter, IDiagnosticCollector diagnostics)
	{
		SignalDirection signalDirection = DirectionSettingForAdjacentBlock(block);
		if (trafficFilter.PreventsSettingRoute(signalDirection))
		{
			diagnostics?.Log($"Interlocking {displayName} direction {signalDirection} prevents setting route {trafficFilter}");
			return true;
		}
		return false;
	}

	private SignalDirection DirectionSettingForAdjacentBlock(CTCBlock block)
	{
		int num = outlets.FindIndex((Outlet outlet) => outlet.Blocks.Any((CTCBlock b) => b == block));
		if (num < 0)
		{
			throw new ArgumentException("Block " + block.name + " is not an outlet on " + base.name);
		}
		Route? route = RouteForCurrentSwitchSettings();
		if (!route.HasValue)
		{
			return SignalDirection.None;
		}
		Route value = route.Value;
		if (value.outletLeft == num || value.outletRight == num)
		{
			return Direction;
		}
		return SignalDirection.None;
	}

	private CTCBlock GetNextIntermediateBlock(CTCBlock block, CTCDirection direction)
	{
		if (block.Intermediate == null)
		{
			return null;
		}
		return block.Intermediate.GetAdjacentTo(block, direction);
	}

	public bool CodeSwitchChanges(List<(TrackNode, SwitchSetting)> switchSettings, out CodeFailureReason reason)
	{
		if (switchSettings == null || switchSettings.Count == 0)
		{
			reason = CodeFailureReason.None;
			return true;
		}
		foreach (CTCBlock block in Blocks)
		{
			if (block.IsOccupied)
			{
				reason = CodeFailureReason.BlockOccupied;
				return false;
			}
		}
		foreach (var (trackNode, switchSetting) in switchSettings)
		{
			if (!TrainController.Shared.CanSetSwitch(trackNode, switchSetting == SwitchSetting.Reversed, out var _))
			{
				Log.Warning("Interlocking {id}: Car on switch {node} but block(s) not occupied", id, trackNode);
				reason = CodeFailureReason.BlockOccupied;
				return false;
			}
		}
		foreach (var switchSetting2 in switchSettings)
		{
			TrackNode item = switchSetting2.Item1;
			SwitchSetting item2 = switchSetting2.Item2;
			bool flag = item2 == SwitchSetting.Reversed;
			if (item.isThrown != flag)
			{
				Debug.Log($"SetSwitch: {item.id}, {item2}");
				StateManager.ApplyLocal(new SetSwitch(item.id, flag, StateManager.Now, "CTC"));
			}
		}
		reason = CodeFailureReason.None;
		return true;
	}

	public (IReadOnlyCollection<CTCBlock>, CTCSignal, bool) BlockAndNexSignal(int routeIndex, CTCDirection direction)
	{
		if (routeIndex < 0 || routeIndex >= routes.Count)
		{
			throw new ArgumentException($"Interlocking {id}: Index out of range: {routeIndex}", "routeIndex");
		}
		Route route = routes[routeIndex];
		Outlet outlet = OutletForDirection(route, direction);
		bool item = IsLined(route);
		return (outlet.Blocks, outlet.nextSignal, item);
	}

	private bool IsLined(Route route)
	{
		for (int i = 0; i < switchSets.Count; i++)
		{
			SwitchSet switchSet = switchSets[i];
			SwitchSetting switchSetting;
			switch ((route.switchFilters.Count >= i) ? route.switchFilters[i] : SwitchFilter.None)
			{
			case SwitchFilter.Normal:
				switchSetting = SwitchSetting.Normal;
				break;
			case SwitchFilter.Reversed:
				switchSetting = SwitchSetting.Reversed;
				break;
			default:
				throw new ArgumentOutOfRangeException();
			case SwitchFilter.None:
				continue;
			}
			foreach (TrackNode switchNode in switchSet.switchNodes)
			{
				if (switchNode.CTCSwitchSetting() != switchSetting)
				{
					return false;
				}
			}
		}
		return true;
	}

	public string DescriptionForRoute(Route route)
	{
		string text = DescriptionForOutlet(route.outletLeft);
		string text2 = DescriptionForOutlet(route.outletRight);
		string text3 = string.Join(", ", route.switchFilters.Select(StringForFilter));
		return "[" + text + "] -- [" + text2 + "] (" + text3 + ")";
	}

	private static string StringForFilter(SwitchFilter f)
	{
		return f switch
		{
			SwitchFilter.Normal => "N", 
			SwitchFilter.Reversed => "R", 
			SwitchFilter.None => "*", 
			_ => throw new ArgumentOutOfRangeException("f", f, null), 
		};
	}

	public string DescriptionForOutlet(int outletIndex)
	{
		if (outletIndex < 0 || outletIndex >= outlets.Count)
		{
			return "<Invalid>";
		}
		Outlet outlet = outlets[outletIndex];
		return DescriptionForOutlet(outlet);
	}

	public string DescriptionForOutlet(Outlet outlet)
	{
		return ((outlet.blocks == null) ? "<null blocks>" : string.Join(", ", outlet.Blocks.Select((CTCBlock b) => (!(b == null)) ? b.name : "<nil>"))) + " (" + ((outlet.direction == CTCDirection.Left) ? "left" : "right") + ")";
	}

	public bool DependsUponSwitch(TrackNode node)
	{
		foreach (SwitchSet switchSet in switchSets)
		{
			foreach (TrackNode switchNode in switchSet.switchNodes)
			{
				if (switchNode == node)
				{
					return true;
				}
			}
		}
		return false;
	}
}
