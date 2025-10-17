using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Game;
using Game.AccessControl;
using Game.State;
using KeyValue.Runtime;
using Model.Ops.Definition;
using UnityEngine;

namespace Model.Ops;

public class IndustryStorageHelper : IPropertyAccessControlDelegate
{
	private readonly IKeyValueObject _keyValueObject;

	private readonly string _id;

	private const string StorageKey = "storage";

	private const string KeyHadUnfulfilledOrders = "hadUnfulfilledOrders";

	private const string KeyInterchangeExtraScheduled = "extraScheduled";

	private const string KeyInterchangeLastServiced = "lastServiced";

	private const string KeyWarnings = "warnings";

	public bool InterchangeDisabled => _keyValueObject["interchangeDisabled"].BoolValue;

	public GameDateTime? InterchangeLastServiced
	{
		get
		{
			if (!_keyValueObject["lastServiced"].IsNull)
			{
				return new GameDateTime((float)_keyValueObject["lastServiced"]);
			}
			return null;
		}
		set
		{
			_keyValueObject["lastServiced"] = value?.KeyValueValue() ?? Value.Null();
		}
	}

	public GameDateTime? InterchangeExtraScheduled
	{
		get
		{
			if (!_keyValueObject["extraScheduled"].IsNull)
			{
				return new GameDateTime((float)_keyValueObject["extraScheduled"]);
			}
			return null;
		}
		set
		{
			_keyValueObject["extraScheduled"] = value?.KeyValueValue() ?? Value.Null();
		}
	}

	public bool CanScheduleExtra => StateManager.CheckAuthorizedToChangeProperty(_id, "extraScheduled");

	public Dictionary<string, string> Warnings
	{
		get
		{
			return _keyValueObject["warnings"].DictionaryValue.ToDictionary((KeyValuePair<string, Value> kv) => kv.Key, (KeyValuePair<string, Value> kv) => kv.Value.StringValue);
		}
		private set
		{
			_keyValueObject["warnings"] = ((value.Count == 0) ? Value.Null() : Value.Dictionary(value.ToDictionary((KeyValuePair<string, string> kv) => kv.Key, (KeyValuePair<string, string> kv) => Value.String(kv.Value))));
		}
	}

	public IndustryStorageHelper(IKeyValueObject keyValueObject, string identifier)
	{
		_keyValueObject = keyValueObject;
		_id = identifier;
		StateManager.Shared.RegisterPropertyObject(_id, keyValueObject, this);
	}

	public void Dispose()
	{
		if (StateManager.Shared != null)
		{
			StateManager.Shared.UnregisterPropertyObject(_id);
		}
	}

	public void AddToStorage(Load load, float quantity, float maxQuantity, string prefix)
	{
		if (float.IsNaN(quantity))
		{
			throw new ArgumentException("NaN quantity", "quantity");
		}
		if (!(quantity < 1E-07f))
		{
			Dictionary<string, Value> dictionary = new Dictionary<string, Value>(_keyValueObject["storage"].DictionaryValue);
			string loadKey = GetLoadKey(prefix, load.id);
			Value value;
			float num = ((!dictionary.TryGetValue(loadKey, out value)) ? 0f : value.FloatValue);
			dictionary[loadKey] = Value.Float(Mathf.Clamp(num + quantity, 0f, maxQuantity));
			_keyValueObject["storage"] = Value.Dictionary(dictionary);
		}
	}

	public float RemoveFromStorage(Load load, float quantity, string prefix = null)
	{
		if (float.IsNaN(quantity))
		{
			throw new ArgumentException("NaN quantity", "quantity");
		}
		if (quantity < 1E-07f)
		{
			return 0f;
		}
		IReadOnlyDictionary<string, Value> dictionaryValue = _keyValueObject["storage"].DictionaryValue;
		string loadKey = GetLoadKey(prefix, load.id);
		if (!dictionaryValue.TryGetValue(loadKey, out var value))
		{
			return 0f;
		}
		float floatValue = value.FloatValue;
		Dictionary<string, Value> dictionary = new Dictionary<string, Value>(dictionaryValue);
		float num = Mathf.Max(0f, floatValue - quantity);
		float result = floatValue - num;
		dictionary[loadKey] = Value.Float(num);
		_keyValueObject["storage"] = Value.Dictionary(dictionary);
		return result;
	}

	public void SetStorage(Load load, float quantity, string prefix = null)
	{
		if (float.IsNaN(quantity))
		{
			throw new ArgumentException("NaN quantity", "quantity");
		}
		Dictionary<string, Value> dictionary = new Dictionary<string, Value>(_keyValueObject["storage"].DictionaryValue);
		string loadKey = GetLoadKey(prefix, load.id);
		dictionary[loadKey] = Value.Float(Mathf.Max(0f, quantity));
		_keyValueObject["storage"] = Value.Dictionary(dictionary);
	}

	public float QuantityInStorage(Load load, string prefix = null)
	{
		string loadKey = GetLoadKey(prefix, load.id);
		if (new Dictionary<string, Value>(_keyValueObject["storage"].DictionaryValue).TryGetValue(loadKey, out var value))
		{
			return value.FloatValue;
		}
		return 0f;
	}

	private static string GetLoadKey(string prefix, string loadId)
	{
		string result = loadId;
		if (!string.IsNullOrEmpty(prefix))
		{
			result = prefix + ":" + loadId;
		}
		return result;
	}

	private static (string prefix, string loadId) ParseLoadKey(string loadKey)
	{
		Match match = Regex.Match(loadKey, "^(?:(.*):)?(.*?)$", RegexOptions.None);
		if (match.Groups.Count < 3)
		{
			throw new ArgumentException("Malformed key", "loadKey");
		}
		return (prefix: match.Groups[1].Value, loadId: match.Groups[2].Value);
	}

	public IEnumerable<Load> Loads()
	{
		CarPrototypeLibrary instance = CarPrototypeLibrary.instance;
		if (instance == null)
		{
			return Array.Empty<Load>();
		}
		IReadOnlyDictionary<string, Value> dict = _keyValueObject["storage"].DictionaryValue;
		IEnumerable<string> loadIds = from loadKey in dict.Keys
			where dict[loadKey].FloatValue > 0f
			select ParseLoadKey(loadKey).loadId;
		return instance.opsLoads.Where((Load load) => loadIds.Contains(load.id));
	}

	public IDisposable ObserveInterchangeLastServicedChanged(Action action)
	{
		return _keyValueObject.Observe("lastServiced", delegate
		{
			action();
		}, callInitial: false);
	}

	public IDisposable ObserveInterchangeExtraScheduledChanged(Action action)
	{
		return _keyValueObject.Observe("extraScheduled", delegate
		{
			action();
		}, callInitial: false);
	}

	public void SetInterchangeDisabled(bool value)
	{
		_keyValueObject["interchangeDisabled"] = Value.Bool(value);
	}

	public void SetWarning(string key, string warning)
	{
		Dictionary<string, string> warnings = Warnings;
		if (string.IsNullOrEmpty(warning))
		{
			warnings.Remove(key);
		}
		else
		{
			warnings[key] = warning;
		}
		Warnings = warnings;
	}

	public AuthorizationRequirementInfo AuthorizationRequirementForPropertyWrite(string key)
	{
		AuthorizationRequirement authorizationRequirement = ((key == "extraScheduled") ? AuthorizationRequirement.MinimumLevelTrainmaster : AuthorizationRequirement.HostOnly);
		return authorizationRequirement;
	}
}
