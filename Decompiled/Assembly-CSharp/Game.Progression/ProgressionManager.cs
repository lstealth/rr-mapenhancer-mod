using System.Linq;
using Game.AccessControl;
using Game.State;
using KeyValue.Runtime;
using Model.Ops;
using Serilog;
using UnityEngine;

namespace Game.Progression;

public class ProgressionManager : GameBehaviour
{
	private Progression[] _progressions;

	private KeyValueObject _keyValueObject;

	private Progression _current;

	public const string ObjectId = "_progression";

	public const string KeyProgression = "progression";

	protected override int EnablePriority => 100;

	private void Awake()
	{
		_progressions = GetComponentsInChildren<Progression>();
		_keyValueObject = base.gameObject.AddComponent<KeyValueObject>();
		StateManager.Shared.RegisterPropertyObject("_progression", _keyValueObject, AuthorizationRequirement.HostOnly);
	}

	private void OnDestroy()
	{
		if (StateManager.Shared != null)
		{
			StateManager.Shared.UnregisterPropertyObject("_progression");
		}
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		if (_current != null)
		{
			_current.Unconfigure();
		}
		_current = null;
	}

	protected override void OnEnableWithProperties()
	{
		string progressionKey = _keyValueObject["progression"].StringValue;
		bool flag = !string.IsNullOrEmpty(progressionKey);
		if (StateManager.IsSandbox && flag)
		{
			Log.Warning("Game is sandbox but has progression {progression}. Ignoring.", progressionKey);
			flag = false;
		}
		if (!flag)
		{
			Log.Information("No progression specified.");
			ProgressionIndustryComponent[] array = Object.FindObjectsOfType<ProgressionIndustryComponent>();
			for (int i = 0; i < array.Length; i++)
			{
				array[i].ProgressionDisabled = true;
			}
		}
		else
		{
			_current = _progressions.FirstOrDefault((Progression p) => p.identifier == progressionKey);
			if (_current == null)
			{
				Log.Error("RR-546 Couldn't find progression {key}", progressionKey);
			}
			else
			{
				_current.Configure(_keyValueObject);
			}
		}
	}
}
