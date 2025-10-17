using Game.Messages;
using Game.State;
using KeyValue.Runtime;
using UnityEngine;

namespace RollingStock.Controls;

public class KeyValuePickableToggle : MonoBehaviour, IPickable
{
	public string displayTitle = "Toggle";

	public string displayMessageTrue = "Click to Close";

	public string displayMessageFalse = "Click to Open";

	public string key;

	[SerializeField]
	internal float maxPickDistance = 50f;

	public const float DefaultMaxPickDistance = 50f;

	private KeyValueObject _keyValueObject;

	public float MaxPickDistance => maxPickDistance;

	public int Priority => 0;

	public TooltipInfo TooltipInfo => new TooltipInfo(displayTitle, TooltipText);

	public PickableActivationFilter ActivationFilter => PickableActivationFilter.PrimaryOnly;

	private bool CurrentValue
	{
		get
		{
			return KeyValueObject.Get(key).BoolValue;
		}
		set
		{
			StateManager.ApplyLocal(PropertyChangeMessage(value));
		}
	}

	private KeyValueObject KeyValueObject
	{
		get
		{
			if (_keyValueObject == null)
			{
				_keyValueObject = GetComponentInParent<KeyValueObject>();
			}
			return _keyValueObject;
		}
	}

	private string TooltipText
	{
		get
		{
			if (IsAuthorized())
			{
				if (!CurrentValue)
				{
					return displayMessageFalse;
				}
				return displayMessageTrue;
			}
			return "<sprite name=\"MouseNo\"> N/A";
		}
	}

	private IGameMessage PropertyChangeMessage(bool targetValue)
	{
		return new PropertyChange(KeyValueObject.RegisteredId, key, new BoolPropertyValue(targetValue));
	}

	private bool IsAuthorized()
	{
		bool currentValue = CurrentValue;
		return StateManager.CheckAuthorizedToSendMessage(PropertyChangeMessage(!currentValue));
	}

	public void Activate(PickableActivateEvent evt)
	{
		CurrentValue = !CurrentValue;
	}

	public void Deactivate()
	{
	}
}
