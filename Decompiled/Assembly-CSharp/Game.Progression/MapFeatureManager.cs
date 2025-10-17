using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using Game.AccessControl;
using Game.Events;
using Game.State;
using KeyValue.Runtime;
using Model.Ops;
using Serilog;
using Track;
using UnityEngine;

namespace Game.Progression;

[DefaultExecutionOrder(-1)]
public class MapFeatureManager : MonoBehaviour
{
	private MapFeature[] _features;

	private KeyValueObject _keyValueObject;

	private IDisposable _keyChangeObserver;

	private Dictionary<string, bool> _cachedFeatureEnables;

	private readonly HashSet<IProgressionDisablable> _progressionDisablabledSet = new HashSet<IProgressionDisablable>();

	private Coroutine _scheduledRebuildCollectionsIndustryDidChange;

	private Coroutine _scheduledRebuildTrack;

	internal const string ObjectId = "mapFeatures";

	private const string FeaturesKey = "features";

	private static MapFeatureManager _instance;

	public IEnumerable<MapFeature> AvailableFeatures => _features;

	public static MapFeatureManager Shared
	{
		get
		{
			if (_instance == null)
			{
				_instance = UnityEngine.Object.FindObjectOfType<MapFeatureManager>();
			}
			return _instance;
		}
	}

	private Dictionary<string, bool> FeatureEnables
	{
		get
		{
			return _keyValueObject["features"].DictionaryValue.ToDictionary((KeyValuePair<string, Value> pair) => pair.Key, (KeyValuePair<string, Value> pair) => pair.Value.BoolValue);
		}
		set
		{
			_keyValueObject["features"] = Value.Dictionary(value.ToDictionary((KeyValuePair<string, bool> pair) => pair.Key, (KeyValuePair<string, bool> pair) => Value.Bool(pair.Value)));
		}
	}

	private void Awake()
	{
		_features = GetComponentsInChildren<MapFeature>();
		KeyValueObject keyValueObject = base.gameObject.AddComponent<KeyValueObject>();
		StateManager.Shared.RegisterPropertyObject("mapFeatures", keyValueObject, AuthorizationRequirement.HostOnly);
		_keyValueObject = keyValueObject;
	}

	private void OnDestroy()
	{
		if (StateManager.Shared != null)
		{
			StateManager.Shared.UnregisterPropertyObject("mapFeatures");
		}
	}

	private void OnEnable()
	{
	}

	private void OnDisable()
	{
		_keyChangeObserver?.Dispose();
		_keyChangeObserver = null;
	}

	private void UpdateCachedEnabledFeatures()
	{
		_cachedFeatureEnables = FeatureEnables;
	}

	public void HandleSnapshotProperties(Dictionary<string, Value> properties, SetValueOrigin origin)
	{
		_keyValueObject.ResetData(properties, origin);
		UpdateCachedEnabledFeatures();
		_keyChangeObserver?.Dispose();
		_keyChangeObserver = _keyValueObject.Observe("features", delegate
		{
			Dictionary<string, bool> featureEnables = FeatureEnables;
			HandleFeatureEnablesChanged(_cachedFeatureEnables, featureEnables, initial: false);
			_cachedFeatureEnables = FeatureEnables;
		}, callInitial: false);
		Dictionary<string, bool> oldValue = new Dictionary<string, bool>();
		HandleFeatureEnablesChanged(oldValue, _cachedFeatureEnables, initial: true);
	}

	private void HandleFeatureEnablesChanged(Dictionary<string, bool> oldValue, Dictionary<string, bool> newValue, bool initial)
	{
		HashSet<MapFeature> hashSet = new HashSet<MapFeature>();
		HashSet<MapFeature> disabledFeatures = new HashSet<MapFeature>();
		bool isSandbox = StateManager.IsSandbox;
		MapFeature[] features = _features;
		foreach (MapFeature mapFeature in features)
		{
			bool flag = mapFeature.defaultEnableInSandbox && isSandbox;
			string identifier = mapFeature.identifier;
			bool value;
			if (initial)
			{
				if (!newValue.TryGetValue(identifier, out value))
				{
					value = flag;
				}
			}
			else
			{
				if (!oldValue.TryGetValue(identifier, out var value2))
				{
					value2 = flag;
				}
				if (!newValue.TryGetValue(identifier, out value))
				{
					value = flag;
				}
				if (value2 == value)
				{
					continue;
				}
			}
			if (value)
			{
				hashSet.Add(mapFeature);
			}
			else
			{
				disabledFeatures.Add(mapFeature);
			}
		}
		Graph shared = Graph.Shared;
		bool flag2 = false;
		bool areaIndustryChanged = false;
		Log.Debug("MapFeatureManager Changed: enabled = {enabled}, disabled = {disabled}", hashSet, disabledFeatures);
		bool flag3 = false;
		foreach (MapFeature item in hashSet)
		{
			flag3 = UpdateFeatureGraphGroups(item, unlocked: true, shared) || flag3;
		}
		foreach (MapFeature item2 in disabledFeatures)
		{
			flag3 = UpdateFeatureGraphGroups(item2, unlocked: false, shared) || flag3;
		}
		if (flag3)
		{
			Log.Information("RebuildCollections: Feature(s) changed graph");
			shared.RebuildCollections();
			flag2 = true;
		}
		List<Industry> externallyExcluded = _features.Where((MapFeature f) => !f.Unlocked || disabledFeatures.Contains(f)).SelectMany((MapFeature f) => f.unlockIncludeIndustries).ToList();
		foreach (MapFeature item3 in hashSet)
		{
			UpdateFeatureForUnlocked(item3, unlocked: true, ref areaIndustryChanged, externallyExcluded);
		}
		foreach (MapFeature item4 in disabledFeatures)
		{
			UpdateFeatureForUnlocked(item4, unlocked: false, ref areaIndustryChanged, new List<Industry>());
		}
		Log.Debug("MapFeatureManager Changed: done");
		if (areaIndustryChanged && _scheduledRebuildCollectionsIndustryDidChange == null)
		{
			_scheduledRebuildCollectionsIndustryDidChange = StartCoroutine(DelayedRebuildCollectionsIndustryDidChange());
		}
		if (flag2)
		{
			Messenger.Default.Send(default(MapFeatureChangedGraph));
			if (_scheduledRebuildTrack == null)
			{
				_scheduledRebuildTrack = StartCoroutine(DelayedRebuildTrack());
			}
		}
	}

	private IEnumerator DelayedRebuildCollectionsIndustryDidChange()
	{
		yield return null;
		_scheduledRebuildCollectionsIndustryDidChange = null;
		Messenger.Default.Send(default(IndustriesDidChange));
	}

	private IEnumerator DelayedRebuildTrack()
	{
		yield return null;
		_scheduledRebuildTrack = null;
		TrackObjectManager.Instance.Rebuild();
	}

	public void SetFeatureEnabled(string featureId, bool unlocked)
	{
		MapFeature mapFeature = _features.FirstOrDefault((MapFeature f) => f.identifier == featureId);
		if (mapFeature == null)
		{
			throw new ArgumentException("No such feature");
		}
		SetFeatureEnabled(mapFeature, unlocked);
	}

	public void SetFeatureEnabled(MapFeature feature, bool unlocked)
	{
		StateManager.AssertIsHost();
		Dictionary<string, bool> featureEnables = FeatureEnables;
		featureEnables[feature.identifier] = unlocked;
		FeatureEnables = featureEnables;
	}

	private void UpdateFeatureForUnlocked(MapFeature feature, bool unlocked, ref bool areaIndustryChanged, IEnumerable<Industry> externallyExcluded)
	{
		feature.Unlocked = unlocked;
		GameObject[] gameObjectsEnableOnUnlock = feature.gameObjectsEnableOnUnlock;
		foreach (GameObject gameObject in gameObjectsEnableOnUnlock)
		{
			if (gameObject == null)
			{
				Log.Warning("Null gameObjectsEnableOnUnlock value in {feature}", feature.identifier);
			}
			else
			{
				gameObject.SetActive(unlocked);
			}
		}
		foreach (IProgressionDisablable item in feature.areasEnableOnUnlock.Where((Area a) => a != null).SelectMany((Area a) => (from value in a.Industries
			where !feature.unlockExcludeIndustries.Contains(value)
			where !externallyExcluded.Contains(value)
			select value).Cast<IProgressionDisablable>().Concat(a.GetComponentsInChildren<PassengerStop>())).Concat(feature.unlockIncludeIndustries)
			.Concat(feature.unlockIncludeIndustryComponents))
		{
			bool flag = !unlocked;
			if (!_progressionDisablabledSet.Contains(item))
			{
				_progressionDisablabledSet.Add(item);
				areaIndustryChanged = true;
			}
			else if (item.ProgressionDisabled != flag)
			{
				areaIndustryChanged = true;
			}
			item.ProgressionDisabled = flag;
		}
	}

	private static bool UpdateFeatureGraphGroups(MapFeature feature, bool unlocked, Graph graph)
	{
		bool flag = false;
		string[] trackGroupsEnableOnUnlock = feature.trackGroupsEnableOnUnlock;
		foreach (string groupId in trackGroupsEnableOnUnlock)
		{
			flag = graph.SetGroupEnabled(groupId, unlocked) || flag;
		}
		trackGroupsEnableOnUnlock = feature.trackGroupsAvailableOnUnlock;
		foreach (string groupId2 in trackGroupsEnableOnUnlock)
		{
			flag = graph.SetGroupAvailable(groupId2, unlocked) || flag;
		}
		return flag;
	}

	public IDisposable ObserveFeaturesChanged(Action action, bool callInitial)
	{
		return _keyValueObject.Observe("features", delegate
		{
			action();
		}, callInitial);
	}

	public void SetFeatureEnables(Dictionary<string, bool> featureEnables)
	{
		Dictionary<string, bool> featureEnables2 = FeatureEnables;
		foreach (KeyValuePair<string, bool> featureEnable in featureEnables)
		{
			featureEnable.Deconstruct(out var key, out var value);
			string key2 = key;
			bool value2 = value;
			featureEnables2[key2] = value2;
		}
		FeatureEnables = featureEnables2;
	}
}
