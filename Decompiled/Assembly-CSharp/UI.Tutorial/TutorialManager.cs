using Game.AccessControl;
using Game.Scripting.Interactive;
using Game.State;
using KeyValue.Runtime;
using Serilog;
using UI.Common;
using UnityEngine;

namespace UI.Tutorial;

public class TutorialManager : MonoBehaviour
{
	private const string ObjectId = "tutorial";

	private const string KeyClosed = "closed";

	private const string KeyComplete = "complete";

	private KeyValueObject _keyValueObject;

	private InteractiveBookWindow _window;

	private static TutorialManager _shared;

	private bool PlayerClosed
	{
		get
		{
			return _keyValueObject["closed"].BoolValue;
		}
		set
		{
			_keyValueObject["closed"] = (value ? ((Value)true) : ((Value)null));
		}
	}

	public bool Complete => _keyValueObject["complete"];

	public static TutorialManager Shared
	{
		get
		{
			if (_shared == null)
			{
				_shared = Object.FindObjectOfType<TutorialManager>() ?? new GameObject("TutorialManager").AddComponent<TutorialManager>();
				_shared.gameObject.hideFlags = HideFlags.DontSave;
			}
			return _shared;
		}
	}

	public void Show()
	{
		if (!_keyValueObject["stack"].IsNull)
		{
			_keyValueObject["stack"] = null;
			ModalAlertController.PresentOkay("The tutorial has changed!", "It looks like this game was started with the original tutorial. If you wish to continue with the tutorial we recommend starting a new Company mode game. We apologize for the inconvenience!\n\nIn Railroader 2025.1 the tutorial was revamped and is not compatible with the original one.");
			return;
		}
		InteractiveBookWindow shared = InteractiveBookWindow.Shared;
		if (!shared.IsShown)
		{
			shared.OnPlayerClosed = delegate
			{
				PlayerClosed = true;
			};
			shared.Show("Tutorial", "tutorial", _keyValueObject);
		}
		_window = shared;
		PlayerClosed = false;
	}

	public void ShowIfAppropriateForLaunch()
	{
		Log.Information("TutorialManager.ShowIfAppropriateForLaunch playerClosed={playerClosed}, complete={complete}", PlayerClosed, Complete);
		if (!PlayerClosed && !Complete)
		{
			Show();
		}
	}

	private void Awake()
	{
		_keyValueObject = base.gameObject.AddComponent<KeyValueObject>();
		StateManager.Shared.RegisterPropertyObject("tutorial", _keyValueObject, AuthorizationRequirement.HostOnly);
	}

	private void OnDestroy()
	{
		if (StateManager.Shared != null)
		{
			StateManager.Shared.UnregisterPropertyObject("tutorial");
		}
	}

	public void HandleConsoleCommand(string[] arguments)
	{
		Show();
		if (arguments.Length != 0)
		{
			_keyValueObject["chapter_id"] = arguments[0];
			if (arguments.Length > 1)
			{
				_keyValueObject["page_id"] = arguments[1];
			}
			_window.RequestReload();
		}
	}
}
