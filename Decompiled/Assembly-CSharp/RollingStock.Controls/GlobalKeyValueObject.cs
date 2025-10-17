using Game.AccessControl;
using Game.State;
using KeyValue.Runtime;
using UnityEngine;

namespace RollingStock.Controls;

[RequireComponent(typeof(KeyValueObject))]
public class GlobalKeyValueObject : MonoBehaviour
{
	public string globalObjectId;

	[SerializeField]
	private AuthorizationRequirement authorizationRequirement = AuthorizationRequirement.MinimumLevelCrew;

	private KeyValueObject _keyValueObject;

	private void OnEnable()
	{
		if (globalObjectId == "")
		{
			Debug.LogWarning(base.name + " has empty globalObjectId: updates will not be propagated");
			return;
		}
		_keyValueObject = GetComponent<KeyValueObject>();
		if (StateManager.Shared != null)
		{
			StateManager.Shared.RegisterPropertyObject(globalObjectId, _keyValueObject, authorizationRequirement);
		}
		else
		{
			Debug.LogWarning("Can't RegisterPropertyObject " + globalObjectId + " -- no StateManager");
		}
	}

	private void OnDisable()
	{
		if (!(globalObjectId == "") && StateManager.Shared != null)
		{
			StateManager.Shared.UnregisterPropertyObject(globalObjectId);
		}
	}
}
