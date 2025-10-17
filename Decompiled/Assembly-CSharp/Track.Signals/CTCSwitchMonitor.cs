using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using Game;
using Game.Events;
using Game.State;
using Serilog;
using UnityEngine;

namespace Track.Signals;

public class CTCSwitchMonitor : GameBehaviour
{
	private Coroutine _observeSwitchesWhenReadyCoroutine;

	private Coroutine _updateSwitchesCoroutine;

	private SignalStorage _storage;

	private readonly HashSet<IDisposable> _observers = new HashSet<IDisposable>();

	private IEnumerable<TrackNode> Switches
	{
		get
		{
			TrainController shared = TrainController.Shared;
			if (shared == null)
			{
				return Array.Empty<TrackNode>();
			}
			Graph graph = shared.graph;
			if (graph == null)
			{
				return Array.Empty<TrackNode>();
			}
			return graph.Nodes.Where((TrackNode node) => graph.IsSwitch(node));
		}
	}

	protected override void OnEnable()
	{
		_storage = GetComponentInParent<SignalStorage>();
		base.OnEnable();
	}

	protected override void OnEnableWithProperties()
	{
		_observers.Add(_storage.ObserveSystemMode(UpdateSwitchesForCTC));
		Messenger.Default.Register<MapFeatureChangedGraph>(this, delegate
		{
			UpdateSwitchesForCTCDelayed();
		});
		Messenger.Default.Register<CTCFeatureChange>(this, delegate
		{
			UpdateSwitchesForCTCDelayed();
		});
		if (StateManager.IsHost)
		{
			_observeSwitchesWhenReadyCoroutine = StartCoroutine(ObserveSwitchesWhenReady());
		}
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		if (_observeSwitchesWhenReadyCoroutine != null)
		{
			StopCoroutine(_observeSwitchesWhenReadyCoroutine);
		}
		_updateSwitchesCoroutine = null;
		foreach (IDisposable observer in _observers)
		{
			observer.Dispose();
		}
		_observers.Clear();
		Messenger.Default.Unregister(this);
	}

	private IEnumerator ObserveSwitchesWhenReady()
	{
		WaitForSeconds wait = new WaitForSeconds(0.5f);
		while (TrainController.Shared == null || TrainController.Shared.graph == null)
		{
			yield return wait;
		}
		ObserveSwitches();
		_observeSwitchesWhenReadyCoroutine = null;
	}

	private void ObserveSwitches()
	{
		if (!StateManager.IsHost)
		{
			return;
		}
		double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
		CTCPanelController shared = CTCPanelController.Shared;
		HashSet<CTCInterlocking> source = shared.AllInterlockings.Values.ToHashSet();
		HashSet<CTCBlock> source2 = shared.AllBlocks.Values.Where((CTCBlock cTCBlock) => cTCBlock.isActiveAndEnabled && cTCBlock.Interlocking == null).ToHashSet();
		foreach (TrackNode node in Switches)
		{
			HashSet<CTCBlock> hashSet = source2.Where((CTCBlock cTCBlock) => cTCBlock.DependsUponSwitchPosition(node)).ToHashSet();
			if (hashSet.Count == 0)
			{
				HashSet<CTCBlock> dependentInterlockingBlocks = source.Where((CTCInterlocking i) => i.DependsUponSwitch(node)).SelectMany((CTCInterlocking i) => i.Blocks).ToHashSet();
				if (!dependentInterlockingBlocks.Any())
				{
					node.OnDidChangeThrown = null;
					continue;
				}
				node.OnDidChangeThrown = delegate
				{
					UpdateSwitchPositionProperty(node);
					foreach (CTCBlock item in dependentInterlockingBlocks)
					{
						item.MarkSwitchNodeUnlocked(node.id, node.IsCTCSwitchUnlocked);
					}
				};
				foreach (CTCBlock item2 in dependentInterlockingBlocks)
				{
					item2.MarkSwitchNodeUnlocked(node.id, node.IsCTCSwitchUnlocked);
				}
				continue;
			}
			if (hashSet.Count > 1)
			{
				Log.Warning("Switch {nodeId} is within multiple blocks, only one will be used: {blocks}", node.id, hashSet.Select((CTCBlock b) => b.id));
			}
			CTCBlock block = hashSet.First();
			node.OnDidChangeThrown = delegate
			{
				UpdateSwitchNodeThrown(node, block);
			};
			UpdateSwitchNodeThrown(node, block);
		}
		double realtimeSinceStartupAsDouble2 = Time.realtimeSinceStartupAsDouble;
		Log.Debug("ObserveSwitches() completed in {dt}", realtimeSinceStartupAsDouble2 - realtimeSinceStartupAsDouble);
	}

	private void UpdateSwitchNodeThrown(TrackNode node, CTCBlock block)
	{
		Log.Information("Switch {nodeId} in block {block} changed to {thrown}", node.id, block.id, node.isThrown);
		UpdateSwitchPositionProperty(node);
		block.MarkSwitchNodeThrown(node.id, node.isThrown);
	}

	private void UpdateSwitchPositionProperty(TrackNode node)
	{
		StateManager.DebugAssertIsHost();
		SwitchSetting switchSetting = (node.isThrown ? SwitchSetting.Reversed : SwitchSetting.Normal);
		_storage.SetSwitchPosition(node.id, switchSetting);
	}

	private void UpdateSwitchesForCTCDelayed()
	{
		if (_updateSwitchesCoroutine != null)
		{
			StopCoroutine(_updateSwitchesCoroutine);
		}
		_updateSwitchesCoroutine = null;
		if (base.gameObject.activeInHierarchy)
		{
			_updateSwitchesCoroutine = StartCoroutine(UpdateSwitchesForCTCDelayedCoroutine());
		}
	}

	private IEnumerator UpdateSwitchesForCTCDelayedCoroutine()
	{
		yield return null;
		_updateSwitchesCoroutine = null;
		UpdateSwitchesForCTC(_storage.SystemMode);
		ObserveSwitches();
	}

	private void UpdateSwitchesForCTC(SystemMode mode)
	{
		HashSet<TrackNode> hashSet = Switches.ToHashSet();
		HashSet<TrackNode> hashSet2 = new HashSet<TrackNode>();
		if (mode == SystemMode.CTC)
		{
			foreach (CTCInterlocking enabledInterlocking in GetEnabledInterlockings())
			{
				foreach (CTCInterlocking.SwitchSet switchSet in enabledInterlocking.switchSets)
				{
					foreach (TrackNode switchNode in switchSet.switchNodes)
					{
						if (hashSet.Contains(switchNode))
						{
							hashSet.Remove(switchNode);
							hashSet2.Add(switchNode);
						}
					}
				}
			}
		}
		TrainController shared = TrainController.Shared;
		if (shared == null)
		{
			return;
		}
		Graph graph = shared.graph;
		foreach (TrackNode item in hashSet)
		{
			if (item.IsCTCSwitch)
			{
				item.IsCTCSwitch = false;
				graph.OnNodeDidChange(item);
			}
		}
		foreach (TrackNode item2 in hashSet2)
		{
			if (!item2.IsCTCSwitch)
			{
				item2.IsCTCSwitch = true;
				graph.OnNodeDidChange(item2);
			}
		}
	}

	private IEnumerable<CTCInterlocking> GetEnabledInterlockings()
	{
		return from i in UnityEngine.Object.FindObjectsOfType<CTCInterlocking>()
			where i.isActiveAndEnabled
			select i;
	}
}
