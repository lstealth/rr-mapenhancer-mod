using System;
using System.Collections;
using System.Collections.Generic;
using Game.State;
using Helpers;
using Model;
using Serilog;
using UnityEngine;

namespace Track.Signals;

public class CTCBlock : MonoBehaviour
{
	public string id;

	public bool thrownSwitchesSetOccupied = true;

	private Coroutine _updateCoroutine;

	private SignalStorage _storage;

	private TrackSpan[] _spans;

	private readonly HashSet<string> _thrownNodeIds = new HashSet<string>();

	private readonly HashSet<string> _unlockedNodeIds = new HashSet<string>();

	private bool _testForceOccupied;

	private SignalStorage Storage => _storage ?? (_storage = GetComponentInParent<SignalStorage>());

	public bool IsOccupied
	{
		get
		{
			if (Storage != null)
			{
				return Storage.GetBlockOccupied(id);
			}
			return false;
		}
	}

	public CTCIntermediate Intermediate { get; private set; }

	public CTCInterlocking Interlocking { get; private set; }

	private bool IsCTC => Storage.SystemMode == SystemMode.CTC;

	public CTCTrafficFilter TrafficFilter
	{
		get
		{
			if (Intermediate == null && IsCTC)
			{
				return Storage.GetBlockTrafficFilter(id);
			}
			return CTCTrafficFilter.Any;
		}
		set
		{
			if (StateManager.IsHost)
			{
				Log.Debug("Block {blockId} traffic filter = {trafficFilter}", id, value);
				if (value == CTCTrafficFilter.Left || value == CTCTrafficFilter.Right)
				{
					_thrownNodeIds.Clear();
				}
				Storage.SetBlockTrafficFilter(id, value);
			}
		}
	}

	private TrackSpan[] Spans => _spans ?? (_spans = GetComponentsInChildren<TrackSpan>());

	private void Awake()
	{
		Intermediate = GetComponentInParent<CTCIntermediate>();
		Interlocking = GetComponentInParent<CTCInterlocking>();
	}

	private void OnEnable()
	{
		if (StateManager.IsHost)
		{
			_updateCoroutine = StartCoroutine(UpdateCoroutine());
		}
	}

	private void OnDisable()
	{
		if (_updateCoroutine != null)
		{
			StopCoroutine(_updateCoroutine);
		}
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = (IsOccupied ? Color.yellow : Color.white);
		Gizmos.DrawSphere(base.transform.position, 0.5f);
	}

	private IEnumerator UpdateCoroutine()
	{
		yield return new WaitForSeconds(UnityEngine.Random.Range(0.1f, 0.5f));
		WaitForSeconds wait = new WaitForSeconds(1f);
		while (true)
		{
			try
			{
				PerformUpdate();
			}
			catch (Exception ex)
			{
				Debug.LogError(ex);
				Log.Error(ex, "Exception during update: {e}", ex);
			}
			yield return wait;
		}
	}

	private void PerformUpdate()
	{
		if (!(Storage == null))
		{
			StateManager.DebugAssertIsHost();
			bool flag = CTCPanelController.Shared.SystemMode == SystemMode.CTC;
			string propertyValue = "(clear)";
			bool flag2 = false;
			if (_testForceOccupied)
			{
				flag2 = true;
				propertyValue = "testForce";
			}
			else if (_thrownNodeIds.Count > 0)
			{
				flag2 = true;
				propertyValue = "thrown switch";
			}
			else if (flag && _unlockedNodeIds.Count > 0)
			{
				flag2 = true;
				propertyValue = "unlocked switch";
			}
			else if (CheckOccupied())
			{
				flag2 = true;
				propertyValue = "occupancy";
			}
			if (IsOccupied != flag2)
			{
				Log.Information("Block {id}: {state}, reason = {reason}", id, flag2 ? "occupied" : "clear", propertyValue);
				Storage.SetBlockOccupied(id, flag2);
			}
		}
	}

	internal void TestForceOccupied(bool occupied)
	{
		_testForceOccupied = occupied;
		PerformUpdate();
	}

	private bool CheckOccupied()
	{
		TrainController shared = TrainController.Shared;
		if (shared == null)
		{
			return false;
		}
		TrackSpan[] spans = Spans;
		bool result = false;
		TrackSpan[] array = spans;
		foreach (TrackSpan span in array)
		{
			if (shared.AnyCarsOnSpan(span))
			{
				result = true;
				break;
			}
		}
		return result;
	}

	public HashSet<Car> CarsInBlock()
	{
		HashSet<Car> hashSet = new HashSet<Car>();
		TrainController shared = TrainController.Shared;
		if (shared == null)
		{
			return hashSet;
		}
		TrackSpan[] spans = Spans;
		foreach (TrackSpan span in spans)
		{
			hashSet.UnionWith(shared.CarsOnSpan(span));
		}
		return hashSet;
	}

	public void DrawGizmos(Color color, float scale)
	{
		TrackSpan[] spans = Spans;
		for (int i = 0; i < spans.Length; i++)
		{
			spans[i].DrawGizmos(color, scale);
		}
	}

	public bool CanSetDirection(CTCDirection propagateDirection, CTCTrafficFilter newTrafficFilter)
	{
		switch (TrafficFilter)
		{
		case CTCTrafficFilter.Left:
			if (propagateDirection != CTCDirection.Left)
			{
				break;
			}
			goto case CTCTrafficFilter.None;
		case CTCTrafficFilter.Right:
			if (propagateDirection != CTCDirection.Right)
			{
				break;
			}
			goto case CTCTrafficFilter.None;
		case CTCTrafficFilter.None:
		case CTCTrafficFilter.Any:
			return true;
		}
		Log.Warning("CanSetDirection {blockId}, propagationDirection {propagation}, newTrafficFilter {newTrafficFilter} - rejected", id, propagateDirection, newTrafficFilter);
		return false;
	}

	public bool TrySetDirection(CTCDirection propagateDirection, CTCTrafficFilter newTrafficFilter, bool dryRun = false)
	{
		if (!CanSetDirection(propagateDirection, newTrafficFilter))
		{
			return false;
		}
		if (!dryRun)
		{
			TrafficFilter = newTrafficFilter;
		}
		return true;
	}

	public bool Contains(Vector3 point)
	{
		TrackSpan[] spans = Spans;
		for (int i = 0; i < spans.Length; i++)
		{
			if (spans[i].Contains(point, 1f))
			{
				return true;
			}
		}
		return false;
	}

	public void MarkSwitchNodeThrown(string nodeId, bool isThrown)
	{
		if (thrownSwitchesSetOccupied && (isThrown ? _thrownNodeIds.Add(nodeId) : _thrownNodeIds.Remove(nodeId)))
		{
			PerformUpdate();
		}
	}

	public void MarkSwitchNodeUnlocked(string nodeId, bool unlocked)
	{
		if (unlocked ? _unlockedNodeIds.Add(nodeId) : _unlockedNodeIds.Remove(nodeId))
		{
			PerformUpdate();
		}
	}

	public bool DependsUponSwitchPosition(TrackNode switchNode)
	{
		if (!thrownSwitchesSetOccupied)
		{
			return false;
		}
		Vector3 point = WorldTransformer.WorldToGame(switchNode.transform.position);
		return Contains(point);
	}

	public override string ToString()
	{
		if (!string.IsNullOrEmpty(id))
		{
			return id;
		}
		return base.name;
	}
}
