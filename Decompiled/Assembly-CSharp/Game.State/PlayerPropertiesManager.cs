using System;
using Game.AccessControl;
using KeyValue.Runtime;
using UnityEngine;

namespace Game.State;

[RequireComponent(typeof(KeyValueObject))]
public class PlayerPropertiesManager : MonoBehaviour
{
	private KeyValueObject _object;

	private const string ObjectId = "players";

	public static PlayerPropertiesManager Shared { get; private set; }

	public PlayerProperties MyProperties
	{
		get
		{
			return new PlayerProperties(_object[PlayersManager.PlayerId.String]);
		}
		set
		{
			_object[PlayersManager.PlayerId.String] = value.Value();
		}
	}

	private void Awake()
	{
		_object = GetComponent<KeyValueObject>();
		StateManager.Shared.RegisterPropertyObject("players", _object, AuthorizationRequirement.PlayerIdKey);
		Shared = this;
	}

	public void OnDestroy()
	{
		if (!StateManager.IsUnloading)
		{
			StateManager.Shared.UnregisterPropertyObject("players");
			Shared = this;
		}
	}

	public void UpdateMyProperties(Func<PlayerProperties, PlayerProperties> action)
	{
		PlayerProperties myProperties = MyProperties;
		MyProperties = action(myProperties);
	}
}
