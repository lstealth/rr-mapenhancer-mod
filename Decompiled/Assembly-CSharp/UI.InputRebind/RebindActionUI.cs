using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UI.InputRebind;

public class RebindActionUI : MonoBehaviour
{
	public Action OnSave;

	[Tooltip("Reference to action that is to be rebound from the UI.")]
	[SerializeField]
	private InputActionReference action;

	[SerializeField]
	private string bindingId;

	[SerializeField]
	private InputBinding.DisplayStringOptions displayStringOptions;

	[Tooltip("Text label that will receive the name of the action. Optional. Set to None to have the rebind UI not show a label for the action.")]
	[SerializeField]
	private TMP_Text actionLabel;

	[Tooltip("Text label that will receive the current, formatted binding string.")]
	[SerializeField]
	private TMP_Text bindingText;

	[Tooltip("Optional UI that will be shown while a rebind is in progress.")]
	[SerializeField]
	private GameObject rebindOverlay;

	[Tooltip("Optional text label that will be updated with prompt for user input.")]
	[SerializeField]
	private TMP_Text rebindText;

	[SerializeField]
	private Button rebindButton;

	[SerializeField]
	private Button resetButton;

	[Tooltip("Event that is triggered when the way the binding is display should be updated. This allows displaying bindings in custom ways, e.g. using images instead of text.")]
	[SerializeField]
	private UpdateBindingUIEvent updateBindingUIEvent;

	[Tooltip("Event that is triggered when an interactive rebind is being initiated. This can be used, for example, to implement custom UI behavior while a rebind is in progress. It can also be used to further customize the rebind.")]
	[SerializeField]
	private InteractiveRebindEvent rebindStartEvent;

	[Tooltip("Event that is triggered when an interactive rebind is complete or has been aborted.")]
	[SerializeField]
	private InteractiveRebindEvent rebindStopEvent;

	private InputActionRebindingExtensions.RebindingOperation _rebindOperation;

	private static List<RebindActionUI> _rebindActionUIs;

	private InputActionReference _createdInputActionReference;

	private bool _conflict;

	private static readonly Dictionary<string, string> ActionStrings = new Dictionary<string, string>
	{
		{ "TogglePlacer", "Equipment Purchase/Placer" },
		{ "Horn", "Whistle/Horn" },
		{ "PushCar", "Rerail/Push" },
		{ "MoveTrain", "Manually Move Selected" },
		{ "Teleport", "Jump to Mouse" }
	};

	public InputActionReference ActionReference
	{
		get
		{
			return action;
		}
		set
		{
			action = value;
			UpdateActionLabel();
			UpdateBindingDisplay();
		}
	}

	public InputAction Action
	{
		get
		{
			return action?.action;
		}
		set
		{
			if (_createdInputActionReference == null)
			{
				_createdInputActionReference = InputActionReference.Create(value);
			}
			else
			{
				_createdInputActionReference.Set(value);
			}
			ActionReference = _createdInputActionReference;
		}
	}

	public string BindingId
	{
		get
		{
			return bindingId;
		}
		set
		{
			bindingId = value;
			UpdateBindingDisplay();
		}
	}

	public InputBinding.DisplayStringOptions DisplayStringOptions
	{
		get
		{
			return displayStringOptions;
		}
		set
		{
			displayStringOptions = value;
			UpdateBindingDisplay();
		}
	}

	public TMP_Text ActionLabel
	{
		get
		{
			return actionLabel;
		}
		set
		{
			actionLabel = value;
			UpdateActionLabel();
		}
	}

	public TMP_Text BindingText
	{
		get
		{
			return bindingText;
		}
		set
		{
			bindingText = value;
			UpdateBindingDisplay();
		}
	}

	public TMP_Text RebindPrompt
	{
		get
		{
			return rebindText;
		}
		set
		{
			rebindText = value;
		}
	}

	public GameObject RebindOverlay
	{
		get
		{
			return rebindOverlay;
		}
		set
		{
			rebindOverlay = value;
		}
	}

	public UpdateBindingUIEvent UpdateBindingUIEvent
	{
		get
		{
			if (updateBindingUIEvent == null)
			{
				updateBindingUIEvent = new UpdateBindingUIEvent();
			}
			return updateBindingUIEvent;
		}
	}

	public InteractiveRebindEvent StartRebindEvent
	{
		get
		{
			if (rebindStartEvent == null)
			{
				rebindStartEvent = new InteractiveRebindEvent();
			}
			return rebindStartEvent;
		}
	}

	public InteractiveRebindEvent StopRebindEvent
	{
		get
		{
			if (rebindStopEvent == null)
			{
				rebindStopEvent = new InteractiveRebindEvent();
			}
			return rebindStopEvent;
		}
	}

	public InputActionRebindingExtensions.RebindingOperation OngoingRebind => _rebindOperation;

	public bool Conflict
	{
		get
		{
			return _conflict;
		}
		set
		{
			_conflict = value;
			ColorBlock colors = rebindButton.colors;
			Color color = (colors.normalColor = (_conflict ? Color.red : Color.white));
			colors.highlightedColor = color * 0.85f;
			rebindButton.colors = colors;
		}
	}

	private void Save()
	{
		GameInput.shared.SaveToPlayerPrefs();
		OnSave?.Invoke();
	}

	public bool ResolveActionAndBinding(out InputAction action, out int bindingIndex)
	{
		bindingIndex = -1;
		action = this.action?.action;
		if (action == null)
		{
			return false;
		}
		if (string.IsNullOrEmpty(this.bindingId))
		{
			return false;
		}
		Guid bindingId = new Guid(this.bindingId);
		bindingIndex = action.bindings.IndexOf((InputBinding x) => x.id == bindingId);
		if (bindingIndex == -1)
		{
			Debug.LogError($"Cannot find binding with ID '{bindingId}' on '{action}'", this);
			return false;
		}
		return true;
	}

	public void UpdateBindingDisplay()
	{
		string text = string.Empty;
		string deviceLayoutName = null;
		string controlPath = null;
		InputAction inputAction = action?.action;
		if (inputAction != null)
		{
			int num = inputAction.bindings.IndexOf((InputBinding x) => x.id.ToString() == bindingId);
			if (num != -1)
			{
				text = inputAction.GetBindingDisplayString(num, out deviceLayoutName, out controlPath, DisplayStringOptions);
			}
			if (Application.isPlaying)
			{
				resetButton.interactable = ShowResetButton(inputAction);
			}
		}
		if (bindingText != null)
		{
			bindingText.text = text;
		}
		updateBindingUIEvent?.Invoke(this, text, deviceLayoutName, controlPath);
	}

	private bool ShowResetButton(InputAction action)
	{
		foreach (InputBinding binding in action.bindings)
		{
			if (binding.hasOverrides)
			{
				return true;
			}
		}
		return false;
	}

	public void ResetToDefault()
	{
		if (!ResolveActionAndBinding(out var inputAction, out var bindingIndex))
		{
			return;
		}
		if (inputAction.bindings[bindingIndex].isComposite)
		{
			for (int i = bindingIndex + 1; i < inputAction.bindings.Count && inputAction.bindings[i].isPartOfComposite; i++)
			{
				inputAction.RemoveBindingOverride(i);
			}
		}
		else
		{
			inputAction.RemoveBindingOverride(bindingIndex);
		}
		UpdateBindingDisplay();
		Save();
	}

	public void StartInteractiveRebind()
	{
		EventSystem.current.SetSelectedGameObject(null);
		if (!ResolveActionAndBinding(out var inputAction, out var bindingIndex))
		{
			return;
		}
		if (inputAction.bindings[bindingIndex].isComposite)
		{
			int num = bindingIndex + 1;
			if (num < inputAction.bindings.Count && inputAction.bindings[num].isPartOfComposite)
			{
				PerformInteractiveRebind(inputAction, num, allCompositeParts: true);
			}
		}
		else
		{
			PerformInteractiveRebind(inputAction, bindingIndex);
		}
	}

	private void PerformInteractiveRebind(InputAction action, int bindingIndex, bool allCompositeParts = false)
	{
		_rebindOperation?.Cancel();
		bool actionWasEnabled = action.enabled;
		if (actionWasEnabled)
		{
			action.Disable();
		}
		_rebindOperation = action.PerformInteractiveRebinding(bindingIndex);
		_rebindOperation.WithControlsExcluding("<Mouse>/leftButton").WithControlsExcluding("<Mouse>/rightButton").WithControlsExcluding("<Mouse>/press")
			.WithCancelingThrough("<Keyboard>/escape")
			.OnCancel(delegate(InputActionRebindingExtensions.RebindingOperation operation)
			{
				RestoreEnabled();
				rebindStopEvent?.Invoke(this, operation);
				SetOverlayVisible(visible: false);
				UpdateBindingDisplay();
				CleanUp();
			})
			.OnComplete(delegate(InputActionRebindingExtensions.RebindingOperation operation)
			{
				RestoreEnabled();
				SetOverlayVisible(visible: false);
				rebindStopEvent?.Invoke(this, operation);
				UpdateBindingDisplay();
				CleanUp();
				if (allCompositeParts)
				{
					int num = bindingIndex + 1;
					if (num < action.bindings.Count && action.bindings[num].isPartOfComposite)
					{
						PerformInteractiveRebind(action, num, allCompositeParts: true);
					}
					else
					{
						Save();
					}
				}
				else
				{
					Save();
				}
			});
		SetOverlayVisible(visible: true);
		if (rebindText != null)
		{
			string compositeBindingName = (action.bindings[bindingIndex].isPartOfComposite ? action.bindings[bindingIndex].name : null);
			string expectedControlType = _rebindOperation.expectedControlType;
			rebindText.text = PromptText(compositeBindingName, expectedControlType);
		}
		if (rebindOverlay == null && rebindText == null && rebindStartEvent == null && bindingText != null)
		{
			bindingText.text = "<Waiting...>";
		}
		rebindStartEvent?.Invoke(this, _rebindOperation);
		_rebindOperation.Start();
		void CleanUp()
		{
			_rebindOperation?.Dispose();
			_rebindOperation = null;
		}
		void RestoreEnabled()
		{
			if (actionWasEnabled)
			{
				action.Enable();
			}
		}
	}

	private static string PromptText(string compositeBindingName, string expectedControlType)
	{
		if (!string.IsNullOrEmpty(compositeBindingName))
		{
			if (compositeBindingName == "modifier")
			{
				return "Waiting for modifier (Shift, Control, Alt) ...";
			}
			if (!(compositeBindingName == "binding"))
			{
				Debug.Log("Unknown compositeBindingName: " + compositeBindingName);
			}
		}
		if (string.IsNullOrEmpty(expectedControlType))
		{
			return "Waiting for input...";
		}
		if (!(expectedControlType == "Button"))
		{
			if (expectedControlType == "Axis")
			{
				return "Waiting for " + compositeBindingName + " button...";
			}
			return "Waiting for " + expectedControlType + "...";
		}
		return "Waiting for button...";
	}

	private void SetOverlayVisible(bool visible)
	{
		if (!(rebindOverlay == null))
		{
			rebindOverlay.SetActive(visible);
			rebindOverlay.transform.SetAsLastSibling();
		}
	}

	private bool CheckDuplicateBindings(InputAction action, int bindingIndex, bool allCompositeParts)
	{
		return false;
	}

	protected void OnEnable()
	{
		if (_rebindActionUIs == null)
		{
			_rebindActionUIs = new List<RebindActionUI>();
		}
		_rebindActionUIs.Add(this);
		if (_rebindActionUIs.Count == 1)
		{
			InputSystem.onActionChange += OnActionChange;
		}
	}

	protected void OnDisable()
	{
		_rebindOperation?.Dispose();
		_rebindOperation = null;
		_rebindActionUIs.Remove(this);
		if (_rebindActionUIs.Count == 0)
		{
			_rebindActionUIs = null;
			InputSystem.onActionChange -= OnActionChange;
		}
	}

	private void OnDestroy()
	{
		if (_createdInputActionReference != null)
		{
			UnityEngine.Object.Destroy(_createdInputActionReference);
			_createdInputActionReference = null;
		}
	}

	private static void OnActionChange(object obj, InputActionChange change)
	{
		if (change != InputActionChange.BoundControlsChanged)
		{
			return;
		}
		InputAction inputAction = obj as InputAction;
		InputActionMap inputActionMap = inputAction?.actionMap ?? (obj as InputActionMap);
		InputActionAsset inputActionAsset = inputActionMap?.asset ?? (obj as InputActionAsset);
		for (int i = 0; i < _rebindActionUIs.Count; i++)
		{
			RebindActionUI rebindActionUI = _rebindActionUIs[i];
			InputAction inputAction2 = rebindActionUI.ActionReference?.action;
			if (inputAction2 != null && (inputAction2 == inputAction || inputAction2.actionMap == inputActionMap || inputAction2.actionMap?.asset == inputActionAsset))
			{
				rebindActionUI.UpdateBindingDisplay();
			}
		}
	}

	private void UpdateActionLabel()
	{
		if (!(actionLabel == null))
		{
			InputAction inputAction = action?.action;
			actionLabel.text = ((inputAction != null) ? ActionString(inputAction.name) : string.Empty);
		}
	}

	private static string ActionString(string actionName)
	{
		if (ActionStrings.TryGetValue(actionName, out var value))
		{
			return value;
		}
		return Regex.Replace(actionName, "(\\B[A-Z])", " $1");
	}
}
