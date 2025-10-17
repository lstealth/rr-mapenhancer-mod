using System.Collections.Generic;
using Game.AccessControl;
using Game.Messages;
using KeyValue.Runtime;
using Network.Messages;
using Serilog;

namespace Game.State;

public class PropertyObjectManager
{
	private struct Record
	{
		public readonly IKeyValueObject Object;

		public readonly IPropertyAccessControlDelegate AccessControlDelegate;

		public readonly SetValueOrigin? RestoreOrigin;

		public Record(IKeyValueObject o, IPropertyAccessControlDelegate accessControlDelegate, SetValueOrigin? restoreOrigin)
		{
			Object = o;
			AccessControlDelegate = accessControlDelegate;
			RestoreOrigin = restoreOrigin;
		}
	}

	private readonly Dictionary<string, Record> _records = new Dictionary<string, Record>();

	public void RegisterPropertyObject(string id, IKeyValueObject keyValueObject, IPropertyAccessControlDelegate accessControlDelegate)
	{
		if (_records.TryGetValue(id, out var value))
		{
			SetValueOrigin? restoreOrigin = value.RestoreOrigin;
			if (restoreOrigin.HasValue)
			{
				SetValueOrigin valueOrDefault = restoreOrigin.GetValueOrDefault();
				keyValueObject.ResetData(value.Object.Dictionary, valueOrDefault);
			}
		}
		keyValueObject.RegisteredId = id;
		_records[id] = new Record(keyValueObject, accessControlDelegate, null);
	}

	public void Unregister(string id)
	{
		_records.Remove(id);
	}

	public void UnregisterAll()
	{
		_records.Clear();
	}

	public AuthorizationRequirementInfo AuthorizationRequirementForPropertyWrite(string id, string key)
	{
		if (!_records.TryGetValue(id, out var value))
		{
			Log.Debug("MinimumAccessLevelForPropertyWrite: Unknown object {id} {key}", id, key);
			return new AuthorizationRequirementInfo(AuthorizationRequirement.HostOnly);
		}
		return value.AccessControlDelegate.AuthorizationRequirementForPropertyWrite(key);
	}

	public void HandlePropertyChange(PropertyChange change)
	{
		if (!_records.TryGetValue(change.ObjectId, out var value))
		{
			Log.Warning("HandlePropertyChange: Unknown object {objectId} {key}", change.ObjectId, change.Key);
			return;
		}
		Value value2 = PropertyValueConverter.SnapshotToRuntime(change.Value);
		value.Object.Set(change.Key, value2, SetValueOrigin.Remote);
	}

	public void PopulateSnapshotForSave(ref Snapshot snapshot)
	{
		foreach (var (key, record2) in _records)
		{
			if (!snapshot.Properties.ContainsKey(key))
			{
				snapshot.Properties[key] = record2.Object.SnapshotValues();
			}
		}
	}

	public void RestoreProperties(Dictionary<string, Dictionary<string, IPropertyValue>> theProperties, SetValueOrigin origin)
	{
		foreach (var (text2, dictionary2) in theProperties)
		{
			if (!_records.TryGetValue(text2, out var value))
			{
				Log.Information("RestoreProperties: Object id {id} not found, saving {propertiesCount} properties", text2, dictionary2.Count);
				_records[text2] = new Record(new KeyValueStorage(PropertyValueConverter.SnapshotToRuntime(dictionary2)), new StaticPropertyAccessControlDelegate(AuthorizationRequirement.HostOnly), origin);
			}
			else
			{
				Dictionary<string, Value> values = PropertyValueConverter.SnapshotToRuntime(dictionary2);
				value.Object.ResetData(values, origin);
			}
		}
	}

	public IKeyValueObject ObjectForIdOrNull(string id)
	{
		if (!_records.TryGetValue(id, out var value))
		{
			return null;
		}
		return value.Object;
	}

	public void HostHandlePropertyChangeRejected(PlayerId playerId, PropertyChange propertyChange)
	{
		IKeyValueObject keyValueObject = ObjectForIdOrNull(propertyChange.ObjectId);
		if (keyValueObject == null)
		{
			Log.Warning("Can't fix rejected property change - no registered object: {id}", propertyChange.ObjectId);
			return;
		}
		IPropertyValue value = PropertyValueConverter.RuntimeToSnapshot(keyValueObject[propertyChange.Key]);
		PropertyChange propertyChange2 = new PropertyChange(propertyChange.ObjectId, propertyChange.Key, value);
		HostManager.Shared.SendTo(playerId, new GameMessageEnvelope(PlayersManager.PlayerId.String, propertyChange2));
	}
}
