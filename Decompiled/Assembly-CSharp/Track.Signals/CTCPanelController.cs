using System;
using System.Collections.Generic;
using System.Linq;
using Audio;
using Game;
using Game.State;
using Helpers;
using JetBrains.Annotations;
using KeyValue.Runtime;
using Serilog;
using UnityEngine;

namespace Track.Signals;

public class CTCPanelController : GameBehaviour
{
	public const string ModeKey = "mode";

	private readonly HashSet<IDisposable> _observers = new HashSet<IDisposable>();

	private readonly HashSet<string> _resetButtonIds = new HashSet<string>();

	private KeyValueObject _keyValueObject;

	private SignalStorage _storage;

	private SystemMode? _lastMode;

	[SerializeField]
	private GameObject ctcGameObject;

	[SerializeField]
	private List<AudioClip> codeButtonAudioClips;

	[SerializeField]
	private IntegerLoopingPlayer relayPlayer;

	private Dictionary<string, CTCInterlocking> _cachedInterlockings;

	private Dictionary<string, CTCBlock> _cachedBlocks;

	private readonly HashSet<IDisposable> _interlockingOccupancyObservers = new HashSet<IDisposable>();

	private CTCPanelButton[] _panelButtons;

	private CTCPanelGroup[] _panelGroups;

	public SystemMode SystemMode => (SystemMode)_keyValueObject["mode"].IntValue;

	[CanBeNull]
	public static CTCPanelController Shared { get; private set; }

	protected override int EnablePriority => 90;

	public IReadOnlyDictionary<string, CTCBlock> AllBlocks
	{
		get
		{
			CacheBlocksIfNeeded();
			return _cachedBlocks;
		}
	}

	public IReadOnlyDictionary<string, CTCInterlocking> AllInterlockings
	{
		get
		{
			CacheInterlockingsIfNeeded();
			return _cachedInterlockings;
		}
	}

	private void Awake()
	{
		Shared = this;
		relayPlayer.ConfigureSource = ConfigurePanelAudioSource;
		_panelButtons = GetComponentsInChildren<CTCPanelButton>(includeInactive: true);
		_panelGroups = GetComponentsInChildren<CTCPanelGroup>(includeInactive: true);
	}

	protected override void OnEnable()
	{
		CheckForDuplicateIds();
		_keyValueObject = GetComponentInParent<KeyValueObject>();
		_storage = GetComponentInParent<SignalStorage>();
		base.OnEnable();
	}

	protected override void OnEnableWithProperties()
	{
		_observers.Add(_storage.ObserveSystemMode(OnModeDidChange));
		_observers.Add(_storage.ObserveUnlockedSwitchIds(HandleUnlockedSwitchIdsDidChange));
		CTCPanelButton[] panelButtons = _panelButtons;
		foreach (CTCPanelButton button in panelButtons)
		{
			_observers.Add(_storage.ObserveButton(button.id, delegate(bool value)
			{
				OnButtonPropertyChange(button, value);
			}));
		}
		if (StateManager.IsHost)
		{
			string key = CTCKeys.Knob("mode");
			_keyValueObject[key] = Value.Int((int)_storage.SystemMode);
			_observers.Add(_keyValueObject.Observe(key, delegate(Value value)
			{
				_storage.SystemMode = (SystemMode)value.IntValue;
			}));
		}
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		foreach (IDisposable observer in _observers)
		{
			observer.Dispose();
		}
		_observers.Clear();
		ClearInterlockingOccupancyObservers();
	}

	private void OnModeDidChange(SystemMode mode)
	{
		if (StateManager.IsHost && (!_lastMode.HasValue || _lastMode != mode))
		{
			ClearAllBlocks();
			ClearInterlockingOccupancyObservers();
			switch (mode)
			{
			case SystemMode.ABS:
				ClearAllRoutes();
				break;
			case SystemMode.CTC:
				StartObservingInterlockingOccupancy();
				break;
			default:
				throw new ArgumentOutOfRangeException("mode", mode, null);
			}
		}
		CTCPanelGroup[] panelGroups = _panelGroups;
		foreach (CTCPanelGroup cTCPanelGroup in panelGroups)
		{
			if (cTCPanelGroup.isActiveAndEnabled)
			{
				cTCPanelGroup.UpdateForMode(mode);
			}
		}
		_lastMode = mode;
	}

	private void ClearInterlockingOccupancyObservers()
	{
		foreach (IDisposable interlockingOccupancyObserver in _interlockingOccupancyObservers)
		{
			interlockingOccupancyObserver.Dispose();
		}
		_interlockingOccupancyObservers.Clear();
	}

	private void StartObservingInterlockingOccupancy()
	{
		CacheInterlockingsIfNeeded();
		foreach (CTCInterlocking value in _cachedInterlockings.Values)
		{
			foreach (CTCBlock block in value.Blocks)
			{
				_interlockingOccupancyObservers.Add(_storage.ObserveBlockOccupancy(block.id, delegate(bool occupied)
				{
					if (occupied)
					{
						ScheduledAudioPlayer.HostPlaySoundAtPosition("ctc-bell", base.transform.GamePosition(), AudioDistance.HyperLocal, AudioController.Group.CTCBell, 5);
					}
				}, callInitial: false));
			}
		}
	}

	public void ClearAllRoutes()
	{
		CacheBlocksIfNeeded();
		CacheInterlockingsIfNeeded();
		Log.Information("Signal mode CTC -> ABS; clearing directions.");
		foreach (string key in _cachedBlocks.Keys)
		{
			_storage.SetBlockTrafficFilter(key, CTCTrafficFilter.None);
		}
		foreach (string key2 in _cachedInterlockings.Keys)
		{
			_storage.SetInterlockingDirection(key2, SignalDirection.None);
		}
	}

	public void ClearAllBlocks()
	{
		CacheBlocksIfNeeded();
		foreach (string key in _cachedBlocks.Keys)
		{
			_storage.SetBlockOccupied(key, value: false);
		}
	}

	private void OnButtonPropertyChange(CTCPanelButton button, bool isPressed)
	{
		if (isPressed)
		{
			if (StateManager.IsHost)
			{
				_resetButtonIds.Add(button.id);
				Invoke("ResetButtonIds", 0.5f);
				Code(button);
			}
			PlayCodeButtonClick();
			relayPlayer.play = true;
			Invoke("StopRelayPlayer", UnityEngine.Random.Range(1.5f, 3f));
		}
	}

	private void Code(CTCPanelButton button)
	{
		StateManager.DebugAssertIsHost();
		CTCPanelGroup componentInParent = button.gameObject.GetComponentInParent<CTCPanelGroup>();
		if (componentInParent == null)
		{
			Log.Error("Couldn't find panel group for button");
			return;
		}
		string interlockingId = componentInParent.interlockingId;
		List<(TrackNode, SwitchSetting)> switchSettings = PanelGroupsForInterlockingId(interlockingId).SelectMany((CTCPanelGroup pg) => pg.switches.Select((TrackNode sw) => (sw: sw, CurrentSwitchSetting: pg.switchKnob.CurrentSwitchSetting))).ToList();
		CTCInterlocking cTCInterlocking = InterlockingForId(interlockingId);
		if (cTCInterlocking == null)
		{
			Log.Error("No such interlocking: {id}", interlockingId);
			return;
		}
		switch (SystemMode)
		{
		case SystemMode.CTC:
		{
			SignalDirection currentDirection = componentInParent.signalKnob.CurrentDirection;
			if (!cTCInterlocking.Code(currentDirection, switchSettings, out var reason2))
			{
				Log.Warning("Code interlocking {id} failed: {reason}", interlockingId, reason2);
			}
			break;
		}
		case SystemMode.ABS:
		{
			if (!cTCInterlocking.CodeSwitchChanges(switchSettings, out var reason))
			{
				Log.Warning("Set switches on interlocking {id} failed: {reason}", interlockingId, reason);
			}
			break;
		}
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	private void StopRelayPlayer()
	{
		relayPlayer.play = false;
	}

	private void PlayCodeButtonClick()
	{
		if (codeButtonAudioClips.Count > 0)
		{
			AudioClip clip = codeButtonAudioClips[UnityEngine.Random.Range(0, codeButtonAudioClips.Count)];
			IAudioSource audioSource = VirtualAudioSourcePool.Checkout("CTCCode", clip, loop: false, AudioController.Group.CTC, 5, base.transform, AudioDistance.Nearby);
			audioSource.volume = 1f;
			ConfigurePanelAudioSource(audioSource);
			audioSource.Play();
			VirtualAudioSourcePool.ReturnAfterFinished(audioSource);
		}
	}

	public static void ConfigurePanelAudioSource(IAudioSource source)
	{
		source.minDistance = 1f;
		source.maxDistance = 7f;
		source.rolloffMode = AudioRolloffMode.Linear;
	}

	private CTCInterlocking InterlockingForId(string id)
	{
		CacheInterlockingsIfNeeded();
		if (_cachedInterlockings.TryGetValue(id, out var value))
		{
			return value;
		}
		return null;
	}

	private T[] GetCTCComponents<T>()
	{
		return ctcGameObject.GetComponentsInChildren<T>(includeInactive: true);
	}

	private void CacheInterlockingsIfNeeded()
	{
		if (_cachedInterlockings == null)
		{
			_cachedInterlockings = new Dictionary<string, CTCInterlocking>();
			CTCInterlocking[] cTCComponents = GetCTCComponents<CTCInterlocking>();
			foreach (CTCInterlocking cTCInterlocking in cTCComponents)
			{
				_cachedInterlockings[cTCInterlocking.id] = cTCInterlocking;
			}
		}
	}

	private void CacheBlocksIfNeeded()
	{
		if (_cachedBlocks == null)
		{
			_cachedBlocks = new Dictionary<string, CTCBlock>();
			CTCBlock[] cTCComponents = GetCTCComponents<CTCBlock>();
			foreach (CTCBlock cTCBlock in cTCComponents)
			{
				_cachedBlocks[cTCBlock.id] = cTCBlock;
			}
		}
	}

	private IEnumerable<CTCPanelGroup> PanelGroupsForInterlockingId(string interlockingId)
	{
		return from @group in base.gameObject.GetComponentsInChildren<CTCPanelGroup>()
			where @group.interlockingId == interlockingId
			select @group;
	}

	private void ResetButtonIds()
	{
		foreach (string resetButtonId in _resetButtonIds)
		{
			_storage.SetButton(resetButtonId, value: false);
		}
		_resetButtonIds.Clear();
	}

	public CTCBlock BlockForId(string blockId)
	{
		CacheBlocksIfNeeded();
		return _cachedBlocks[blockId];
	}

	private bool IsOccupied(CTCBlock block)
	{
		return _storage.GetBlockOccupied(block.id);
	}

	private bool IsOccupied(IEnumerable<CTCBlock> blocks)
	{
		return blocks.Any(IsOccupied);
	}

	private static void CheckForDuplicateIds()
	{
	}

	public void SetSwitchUnlocked(string nodeId, bool unlocked)
	{
		_storage.SetSwitchIdUnlocked(nodeId, unlocked);
	}

	private void HandleUnlockedSwitchIdsDidChange(string[] nodsIdsArray)
	{
		HashSet<string> hashSet = nodsIdsArray.ToHashSet();
		foreach (CTCInterlocking value in AllInterlockings.Values)
		{
			foreach (CTCInterlocking.SwitchSet switchSet in value.switchSets)
			{
				foreach (TrackNode switchNode in switchSet.switchNodes)
				{
					bool flag = hashSet.Contains(switchNode.id);
					if (flag != switchNode.IsCTCSwitchUnlocked)
					{
						switchNode.IsCTCSwitchUnlocked = flag;
						switchNode.OnDidChangeThrown?.Invoke();
					}
				}
			}
		}
	}

	public void CodeSwitchAndSignal(string interlockingId, SwitchSetting switchSetting, SignalDirection direction)
	{
		CTCPanelGroup cTCPanelGroup = _panelGroups.FirstOrDefault((CTCPanelGroup pg) => pg.interlockingId == interlockingId);
		if (cTCPanelGroup == null)
		{
			throw new Exception("Interlocking " + interlockingId + " not found");
		}
		if (cTCPanelGroup.switchKnob != null)
		{
			cTCPanelGroup.switchKnob.SetSwitchSetting(switchSetting);
		}
		if (cTCPanelGroup.signalKnob != null)
		{
			cTCPanelGroup.signalKnob.SetSignalDirection(direction);
		}
		CTCPanelColumnPrefab component = cTCPanelGroup.GetComponent<CTCPanelColumnPrefab>();
		Code(component.codeButton);
	}
}
