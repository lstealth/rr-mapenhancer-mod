using System;
using System.Collections.Generic;
using System.Linq;
using Character;
using Game;
using Game.Messages;
using Game.State;
using Helpers;
using Model;
using Network;
using RollingStock;
using Serilog;
using TMPro;
using UI.Common;
using UI.CompanyWindow;
using UI.EngineControls;
using UI.EngineRoster;
using UI.Equipment;
using UI.Guide;
using UI.Map;
using UI.Placer;
using UI.PreferencesWindow;
using UI.SwitchList;
using UI.Timetable;
using UI.Tooltips;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace UI;

public class GameInput : MonoBehaviour
{
	private enum InputMode
	{
		Move,
		UI
	}

	public enum EscapeHandler
	{
		Pause,
		Transient,
		QuickSearch
	}

	private static InputMode _inputMode = InputMode.Move;

	[SerializeField]
	private PressInput pressInput;

	[SerializeField]
	public InputActionAsset inputActions;

	private InputActionMap _gameActionMap;

	private bool _focusPause;

	private InputAction _help;

	private InputAction _cameraJumpToSeat;

	private InputAction _cameraJumpToHead;

	private InputAction _cameraJumpToTail;

	private InputAction _cameraFollowHead;

	private InputAction _cameraFollowTail;

	private InputAction _cameraSelectFirstPerson;

	private InputAction _cameraSelectStrategy;

	private InputAction _cameraSelectDispatcher;

	private InputAction _cameraJumpStrategyToAvatar;

	private InputAction _crouchAction;

	private InputAction _jumpAction;

	private InputAction _leanLeftAction;

	private InputAction _leanRightAction;

	internal InputAction _moveAction;

	internal InputAction _runAction;

	private InputAction _veryFastAction;

	internal InputAction _teleportAction;

	private InputAction _placeFlareAction;

	private InputAction _hornAction;

	private InputAction _hornExpressionEnableAction;

	private InputAction _hornExpressionValueAction;

	private InputAction _autoEngineerWaypointSelect;

	private InputAction _headlightForwardAction;

	private InputAction _headlightBackAction;

	private InputAction _reverserForward;

	private InputAction _reverserBack;

	private InputAction _throttleUp;

	private InputAction _throttleDown;

	private InputAction _trainBrakeApply;

	private InputAction _trainBrakeRelease;

	private InputAction _locomotiveBrakeApply;

	private InputAction _locomotiveBrakeRelease;

	private InputAction _bell;

	private InputAction _cylinderCock;

	private InputAction _resetFOVAction;

	private InputAction _showPlacerAction;

	private InputAction _toggleMapAction;

	private InputAction _toggleTagsAction;

	private InputAction _toggleSwitchListAction;

	private InputAction _toggleTimetableAction;

	private InputAction _toggleTimeWindowAction;

	private InputAction _toggleCompanyWindowAction;

	private InputAction _togglePreferencesWindowAction;

	private InputAction _toggleLanternAction;

	private InputAction _pushCarAction;

	private InputAction _togglePhotoModeAction;

	private InputAction _toggleConsoleAction;

	private InputAction _toggleEngineRosterAction;

	private InputAction _queryAction;

	private InputAction _copyLocationAction;

	private InputAction _toggleContextMenuAction;

	private InputAction _moveTrainAction;

	private InputAction _recallSelectionAction;

	private InputAction _cycleSelectionAction;

	private InputAction _quickSearchAction;

	private InputAction _closeWindowAction;

	private InputAction _showPauseMenuAction;

	private InputAction _ffwdUpAction;

	private InputAction _ffwdDownAction;

	[NonSerialized]
	public Vector4 MovementCounter;

	[NonSerialized]
	public bool MovementJumped;

	[NonSerialized]
	public bool EnableMovementCounters;

	private const string KeyRebinds = "rebinds";

	private VirtualRepeatingInput _reverserForwardSlowRepeating;

	private VirtualRepeatingInput _reverserBackSlowRepeating;

	private VirtualRepeatingInput _throttleUpSlowRepeating;

	private VirtualRepeatingInput _throttleDownSlowRepeating;

	private VirtualRepeatingInput _reverserForwardRepeating;

	private VirtualRepeatingInput _reverserBackRepeating;

	private VirtualRepeatingInput _throttleUpRepeating;

	private VirtualRepeatingInput _throttleDownRepeating;

	private VirtualRepeatingInput _trainBrakeApplyRepeating;

	private VirtualRepeatingInput _trainBrakeReleaseRepeating;

	private VirtualRepeatingInput _locomotiveBrakeApplyRepeating;

	private VirtualRepeatingInput _locomotiveBrakeReleaseRepeating;

	private static readonly List<RaycastResult> raycastHits = new List<RaycastResult>(4);

	private static readonly Dictionary<EscapeHandler, Func<bool>> _escapeHandlers = new Dictionary<EscapeHandler, Func<bool>>();

	public static bool MovementInputEnabled => _inputMode == InputMode.Move;

	public static GameInput shared { get; private set; }

	public (string title, InputAction[] actions)[] RebindableActions { get; private set; }

	public Vector2 MoveVector => _moveAction.ReadValue<Vector2>();

	public bool ModifierRun => _runAction.IsPressed();

	public bool ModifierVeryFast => _veryFastAction.IsPressed();

	public bool Teleport => _teleportAction.WasPerformedThisFrame();

	public bool PlaceFlare => _placeFlareAction.WasPerformedThisFrame();

	public bool HornExpressionEnabledThisFrame => _hornExpressionEnableAction.WasPerformedThisFrame();

	public bool HornExpressionEnabled => _hornExpressionEnableAction.IsPressed();

	public float HornExpressionValue => _hornExpressionValueAction.ReadValue<float>();

	public int InputHeadlight
	{
		get
		{
			if (_headlightForwardAction.WasPerformedThisFrame())
			{
				return 1;
			}
			if (_headlightBackAction.WasPerformedThisFrame())
			{
				return -1;
			}
			return 0;
		}
	}

	public float InputHorn
	{
		get
		{
			bool isShiftDown = IsShiftDown;
			if (_hornAction.IsPressed())
			{
				if (!isShiftDown)
				{
					return 0.3f;
				}
				return 1f;
			}
			return 0f;
		}
	}

	public bool ShowHelp => _help.WasPerformedThisFrame();

	public bool CameraJumpToSeat => _cameraJumpToSeat.WasPerformedThisFrame();

	public bool CameraJumpToHead => _cameraJumpToHead.WasPerformedThisFrame();

	public bool CameraJumpToTail => _cameraJumpToTail.WasPerformedThisFrame();

	public bool CameraFollowHead => _cameraFollowHead.WasPerformedThisFrame();

	public bool CameraFollowTail => _cameraFollowTail.WasPerformedThisFrame();

	public bool CameraSelectFirstPerson => _cameraSelectFirstPerson.WasPerformedThisFrame();

	public bool CameraSelectStrategy => _cameraSelectStrategy.WasPerformedThisFrame();

	public bool CameraSelectDispatcher => _cameraSelectDispatcher.WasPerformedThisFrame();

	public bool CameraJumpStrategyToAvatar => _cameraJumpStrategyToAvatar.WasPerformedThisFrame();

	public bool CrouchDown => _crouchAction.WasPerformedThisFrame();

	public bool CrouchUp => _crouchAction.WasReleasedThisFrame();

	public bool JumpDown => _jumpAction.WasPerformedThisFrame();

	public bool PrimaryPressStartedThisFrame => pressInput.PrimaryPressStartedThisFrame;

	public bool PrimaryPressEndedThisFrame => pressInput.PrimaryPressEndedThisFrame;

	public bool MouseLookToggle
	{
		get
		{
			if (Preferences.MouseLookToggle)
			{
				return pressInput.SecondaryPressedThisFrame;
			}
			return false;
		}
	}

	public bool ActivateSecondary
	{
		get
		{
			if (Preferences.MouseLookToggle || !pressInput.SecondaryPressedThisFrame)
			{
				return _toggleContextMenuAction.WasPerformedThisFrame();
			}
			return true;
		}
	}

	public bool SecondaryLongPressBeganThisFrame => pressInput.SecondaryLongPressBeganThisFrame;

	public bool SecondaryLongPressEndedThisFrame => pressInput.SecondaryLongPressEndedThisFrame;

	public Vector2 LookDelta => new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

	public float ZoomDelta
	{
		get
		{
			if (!IsMouseOverGameWindow())
			{
				return 0f;
			}
			if (IsMouseOverUI(out var _, out var _))
			{
				return 0f;
			}
			return Input.mouseScrollDelta.y;
		}
	}

	public bool LeanLeft => _leanLeftAction.IsPressed();

	public bool LeanRight => _leanRightAction.IsPressed();

	public bool InputResetFOV => _resetFOVAction.WasPerformedThisFrame();

	private bool InputShowPlacer => _showPlacerAction.WasPerformedThisFrame();

	private bool InputToggleMap => _toggleMapAction.WasPerformedThisFrame();

	public bool InputToggleTags => _toggleTagsAction.WasPerformedThisFrame();

	private bool InputToggleSwitchList => _toggleSwitchListAction.WasPerformedThisFrame();

	private bool InputToggleTimetable => _toggleTimetableAction.WasPerformedThisFrame();

	private bool InputToggleTimeWindow => _toggleTimeWindowAction.WasPerformedThisFrame();

	private bool InputToggleCompanyWindow => _toggleCompanyWindowAction.WasPerformedThisFrame();

	private bool InputTogglePreferencesWindow => _togglePreferencesWindowAction.WasPerformedThisFrame();

	private bool InputToggleLantern => _toggleLanternAction.WasPerformedThisFrame();

	private bool InputPushCar => _pushCarAction.WasPerformedThisFrame();

	public bool InputTogglePhotoMode => _togglePhotoModeAction.WasPerformedThisFrame();

	public bool InputToggleConsole => _toggleConsoleAction.WasPerformedThisFrame();

	public bool Query => _queryAction.IsPressed();

	public bool CopyLocation => _copyLocationAction.IsPressed();

	public bool InputFfwdUp => _ffwdUpAction.WasPerformedThisFrame();

	public bool InputFfwdDown => _ffwdDownAction.WasPerformedThisFrame();

	public static bool IsShiftDown
	{
		get
		{
			if (!Input.GetKey(KeyCode.LeftShift))
			{
				return Input.GetKey(KeyCode.RightShift);
			}
			return true;
		}
	}

	public static bool IsControlDown
	{
		get
		{
			if (!Input.GetKey(KeyCode.LeftControl))
			{
				return Input.GetKey(KeyCode.RightControl);
			}
			return true;
		}
	}

	private static bool IsAltDown
	{
		get
		{
			if (!Input.GetKey(KeyCode.LeftAlt))
			{
				return Input.GetKey(KeyCode.RightAlt);
			}
			return true;
		}
	}

	public static bool SmartAirHelperModifier
	{
		get
		{
			if (IsShiftDown)
			{
				return !IsControlDown;
			}
			return false;
		}
	}

	public bool ReverserForwardRepeating => _reverserForwardRepeating.ActiveThisFrame();

	public bool ReverserBackRepeating => _reverserBackRepeating.ActiveThisFrame();

	public bool ThrottleUpRepeating => _throttleUpRepeating.ActiveThisFrame();

	public bool ThrottleDownRepeating => _throttleDownRepeating.ActiveThisFrame();

	public bool ReverserForward => _reverserForwardSlowRepeating.ActiveThisFrame();

	public bool ReverserBack => _reverserBackSlowRepeating.ActiveThisFrame();

	public bool ThrottleUp => _throttleUpSlowRepeating.ActiveThisFrame();

	public bool ThrottleDown => _throttleDownSlowRepeating.ActiveThisFrame();

	public bool TrainBrakeApply => _trainBrakeApplyRepeating.ActiveThisFrame();

	public bool TrainBrakeRelease => _trainBrakeReleaseRepeating.ActiveThisFrame();

	public bool LocomotiveBrakeApply => _locomotiveBrakeApplyRepeating.ActiveThisFrame();

	public bool LocomotiveBrakeRelease => _locomotiveBrakeReleaseRepeating.ActiveThisFrame();

	public bool Bell => _bell.WasPerformedThisFrame();

	public bool CylinderCock => _cylinderCock.WasPerformedThisFrame();

	private void Awake()
	{
		shared = this;
		_gameActionMap = inputActions.FindActionMap("Game", throwIfNotFound: true);
		_help = inputActions["Game/Help"];
		_cameraJumpToSeat = inputActions["Game/CameraJumpToSeat"];
		_cameraJumpToHead = inputActions["Game/CameraJumpToHead"];
		_cameraJumpToTail = inputActions["Game/CameraJumpToTail"];
		_cameraFollowHead = inputActions["Game/CameraFollowHead"];
		_cameraFollowTail = inputActions["Game/CameraFollowTail"];
		_cameraSelectFirstPerson = inputActions["Game/CameraSelectFirstPerson"];
		_cameraSelectStrategy = inputActions["Game/CameraSelectOverhead"];
		_cameraSelectDispatcher = inputActions["Game/CameraSelectDispatcher"];
		_cameraJumpStrategyToAvatar = inputActions["Game/CameraOverheadToCharacter"];
		_crouchAction = inputActions["Game/Crouch"];
		_jumpAction = inputActions["Game/Jump"];
		_leanLeftAction = inputActions["Game/LeanLeft"];
		_leanRightAction = inputActions["Game/LeanRight"];
		_moveAction = inputActions["Game/Move"];
		_runAction = inputActions["Game/Run"];
		_veryFastAction = inputActions["Game/VeryFast"];
		_placeFlareAction = inputActions["Game/PlaceFlare"];
		_teleportAction = inputActions["Game/Teleport"];
		_hornAction = inputActions["Game/Horn"];
		_headlightForwardAction = inputActions["Game/HeadlightNext"];
		_headlightBackAction = inputActions["Game/HeadlightPrevious"];
		_hornExpressionEnableAction = inputActions["Game/HornExpressionEnable"];
		_hornExpressionValueAction = inputActions["Game/HornExpressionValue"];
		_autoEngineerWaypointSelect = inputActions["Game/AutoEngineerWaypointSelect"];
		_reverserForward = inputActions["Game/ReverserForward"];
		_reverserBack = inputActions["Game/ReverserBack"];
		_throttleUp = inputActions["Game/ThrottleUp"];
		_throttleDown = inputActions["Game/ThrottleDown"];
		_trainBrakeApply = inputActions["Game/TrainBrakeApply"];
		_trainBrakeRelease = inputActions["Game/TrainBrakeRelease"];
		_locomotiveBrakeApply = inputActions["Game/LocomotiveBrakeApply"];
		_locomotiveBrakeRelease = inputActions["Game/LocomotiveBrakeRelease"];
		_bell = inputActions["Game/Bell"];
		_cylinderCock = inputActions["Game/CylinderCock"];
		_resetFOVAction = inputActions["Game/ResetFieldOfView"];
		_showPlacerAction = inputActions["Game/TogglePlacer"];
		_toggleMapAction = inputActions["Game/ToggleMap"];
		_toggleTagsAction = inputActions["Game/ToggleTags"];
		_toggleSwitchListAction = inputActions["Game/ToggleSwitchList"];
		_toggleTimetableAction = inputActions["Game/ToggleTimetable"];
		_toggleTimeWindowAction = inputActions["Game/ToggleTimeWindow"];
		_toggleCompanyWindowAction = inputActions["Game/ToggleCompanyWindow"];
		_togglePreferencesWindowAction = inputActions["Game/TogglePreferencesWindow"];
		_toggleLanternAction = inputActions["Game/ToggleLantern"];
		_pushCarAction = inputActions["Game/PushCar"];
		_togglePhotoModeAction = inputActions["Game/TogglePhotoMode"];
		_toggleConsoleAction = inputActions["Game/ToggleConsole"];
		_toggleEngineRosterAction = inputActions["Game/ToggleEngineRoster"];
		_queryAction = inputActions["Game/Query"];
		_copyLocationAction = inputActions["Game/CopyLocationToClipboard"];
		_toggleContextMenuAction = inputActions["Game/ToggleContextMenu"];
		_moveTrainAction = inputActions["Game/MoveTrain"];
		_recallSelectionAction = inputActions["Game/RecallSelection"];
		_cycleSelectionAction = inputActions["Game/CycleSelection"];
		_quickSearchAction = inputActions["Game/QuickSearch"];
		_ffwdUpAction = inputActions["Game/FastForwardUp"];
		_ffwdDownAction = inputActions["Game/FastForwardDown"];
		_closeWindowAction = inputActions["Game/CloseWindow"];
		_showPauseMenuAction = inputActions["Global/ShowPauseMenu"];
		RebindableActions = new(string, InputAction[])[4]
		{
			("Movement", new InputAction[11]
			{
				_moveAction, _runAction, _veryFastAction, _jumpAction, _teleportAction, _leanLeftAction, _leanRightAction, _placeFlareAction, _resetFOVAction, _toggleLanternAction,
				_queryAction
			}),
			("Camera", new InputAction[9] { _cameraSelectFirstPerson, _cameraSelectStrategy, _cameraSelectDispatcher, _cameraJumpStrategyToAvatar, _cameraFollowHead, _cameraFollowTail, _cameraJumpToHead, _cameraJumpToTail, _cameraJumpToSeat }),
			("UI", new InputAction[13]
			{
				_help, _closeWindowAction, _toggleMapAction, _toggleCompanyWindowAction, _togglePreferencesWindowAction, _toggleEngineRosterAction, _toggleTagsAction, _toggleSwitchListAction, _toggleTimetableAction, _toggleTimeWindowAction,
				_togglePhotoModeAction, _toggleConsoleAction, _toggleContextMenuAction
			}),
			("Equipment", new InputAction[19]
			{
				_showPlacerAction, _hornAction, _bell, _headlightForwardAction, _headlightBackAction, _cylinderCock, _pushCarAction, _reverserForward, _reverserBack, _throttleUp,
				_throttleDown, _trainBrakeApply, _trainBrakeRelease, _locomotiveBrakeApply, _locomotiveBrakeRelease, _moveTrainAction, _recallSelectionAction, _cycleSelectionAction, _autoEngineerWaypointSelect
			})
		};
	}

	private void OnEnable()
	{
		inputActions.Enable();
		LoadFromPreferences();
		_reverserForwardSlowRepeating = new VirtualRepeatingInput(_reverserForward, 0.25f);
		_reverserBackSlowRepeating = new VirtualRepeatingInput(_reverserBack, 0.25f);
		_throttleUpSlowRepeating = new VirtualRepeatingInput(_throttleUp, 0.25f);
		_throttleDownSlowRepeating = new VirtualRepeatingInput(_throttleDown, 0.25f);
		_reverserForwardRepeating = new VirtualRepeatingInput(_reverserForward);
		_reverserBackRepeating = new VirtualRepeatingInput(_reverserBack);
		_throttleUpRepeating = new VirtualRepeatingInput(_throttleUp);
		_throttleDownRepeating = new VirtualRepeatingInput(_throttleDown);
		_trainBrakeApplyRepeating = new VirtualRepeatingInput(_trainBrakeApply);
		_trainBrakeReleaseRepeating = new VirtualRepeatingInput(_trainBrakeRelease);
		_locomotiveBrakeApplyRepeating = new VirtualRepeatingInput(_locomotiveBrakeApply);
		_locomotiveBrakeReleaseRepeating = new VirtualRepeatingInput(_locomotiveBrakeRelease);
	}

	private void OnDisable()
	{
		inputActions.Disable();
		_reverserForwardSlowRepeating.Dispose();
		_reverserBackSlowRepeating.Dispose();
		_throttleUpSlowRepeating.Dispose();
		_throttleDownSlowRepeating.Dispose();
		_reverserForwardRepeating.Dispose();
		_reverserBackRepeating.Dispose();
		_throttleUpRepeating.Dispose();
		_throttleDownRepeating.Dispose();
		_trainBrakeApplyRepeating.Dispose();
		_trainBrakeReleaseRepeating.Dispose();
		_locomotiveBrakeApplyRepeating.Dispose();
		_locomotiveBrakeReleaseRepeating.Dispose();
	}

	private void Update()
	{
		GameObject currentSelectedGameObject = EventSystem.current.currentSelectedGameObject;
		bool flag = currentSelectedGameObject != null && currentSelectedGameObject.GetComponent<TMP_InputField>() != null;
		InputMode inputMode = _inputMode;
		_inputMode = ((flag || _focusPause) ? InputMode.UI : InputMode.Move);
		if (inputMode != _inputMode)
		{
			Log.Information("InputMode {newMode}; {hasSelection} {selected}", _inputMode, flag, (currentSelectedGameObject != null) ? currentSelectedGameObject.transform.HierarchyString() : "<null>");
			if (_inputMode == InputMode.Move)
			{
				_gameActionMap.Enable();
			}
			else
			{
				_gameActionMap.Disable();
			}
		}
		if (_showPauseMenuAction.WasPerformedThisFrame())
		{
			HandleEscape();
		}
		if (_closeWindowAction.WasPerformedThisFrame())
		{
			WindowManager.Shared.CloseTopmostWindow();
		}
		if (!MovementInputEnabled)
		{
			return;
		}
		if (InputToggleMap)
		{
			MapWindow.Toggle();
		}
		if (InputShowPlacer)
		{
			if (StateManager.IsSandbox)
			{
				PlacerWindow.Toggle();
			}
			else
			{
				EquipmentWindow.Toggle();
			}
		}
		if (ShowHelp)
		{
			GuideWindow.Toggle();
		}
		if (InputToggleSwitchList)
		{
			SwitchListPanel.Toggle();
		}
		if (_toggleEngineRosterAction.WasPerformedThisFrame())
		{
			EngineRosterPanel.Toggle();
		}
		if (InputToggleCompanyWindow)
		{
			UI.CompanyWindow.CompanyWindow.Toggle();
		}
		if (InputTogglePreferencesWindow)
		{
			UI.PreferencesWindow.PreferencesWindow.Toggle();
		}
		if (InputToggleTimetable)
		{
			TimetableWindow.Toggle();
		}
		if (InputToggleTimeWindow)
		{
			TimeWindow.Toggle();
		}
		if (InputToggleLantern)
		{
			LocalAvatar localAvatar = CameraSelector.shared.localAvatar;
			localAvatar.LanternEnabled = !localAvatar.LanternEnabled;
			Multiplayer.Client?.SendCharacter();
		}
		if (InputPushCar)
		{
			PushCar();
		}
		if (_moveTrainAction.WasPerformedThisFrame())
		{
			ConsistPlacer.MoveSelectedTrain();
		}
		if (_autoEngineerWaypointSelect.WasPerformedThisFrame())
		{
			AutoEngineerSetWaypoint();
		}
		if (_recallSelectionAction.WasPerformedThisFrame())
		{
			RecallSelection();
		}
		if (_cycleSelectionAction.WasPerformedThisFrame())
		{
			CycleSelection();
		}
		if (EnableMovementCounters)
		{
			Vector2 moveVector = MoveVector;
			float deltaTime = Time.deltaTime;
			if (moveVector.y > 0f)
			{
				MovementCounter.x += moveVector.y * deltaTime;
			}
			if (moveVector.y < 0f)
			{
				MovementCounter.y -= moveVector.y * deltaTime;
			}
			if (moveVector.x < 0f)
			{
				MovementCounter.z -= moveVector.x * deltaTime;
			}
			if (moveVector.x > 0f)
			{
				MovementCounter.w += moveVector.x * deltaTime;
			}
			MovementJumped = MovementJumped || JumpDown;
		}
	}

	private void LoadFromPreferences()
	{
		string text = PlayerPrefs.GetString("rebinds");
		if (!string.IsNullOrEmpty(text))
		{
			inputActions.LoadBindingOverridesFromJson(text);
		}
	}

	public void SaveToPlayerPrefs()
	{
		string value = inputActions.SaveBindingOverridesAsJson();
		PlayerPrefs.SetString("rebinds", value);
	}

	public Vector3 GetMovement(float normalSpeed, float fastSpeed, float fasterSpeed)
	{
		if (!MovementInputEnabled)
		{
			return Vector3.zero;
		}
		float num = normalSpeed;
		if (ModifierVeryFast)
		{
			num = fasterSpeed;
		}
		else if (ModifierRun)
		{
			num = fastSpeed;
		}
		Vector2 moveVector = MoveVector;
		Vector3 vector = new Vector3(moveVector.x, 0f, moveVector.y);
		if (LeanLeft)
		{
			vector += Vector3.up;
		}
		if (LeanRight)
		{
			vector -= Vector3.up;
		}
		vector = vector.normalized;
		return vector * num;
	}

	public GameInput(bool movementJumped)
	{
		MovementJumped = movementJumped;
	}

	public void SetPaused(bool paused)
	{
		_focusPause = paused;
	}

	public static bool IsMouseOverGameWindow(Window window = null)
	{
		Vector2 vector = Mouse.current.position.ReadValue();
		if (0f > vector.x || 0f > vector.y || (float)Screen.width < vector.x || (float)Screen.height < vector.y)
		{
			return false;
		}
		return WindowManager.Shared.HitTest(vector) == window;
	}

	public static bool IsMouseOverUI(out TooltipInfo tooltipInfo, out string debugInfo)
	{
		tooltipInfo = default(TooltipInfo);
		EventSystem current = EventSystem.current;
		debugInfo = null;
		if (!current.IsPointerOverGameObject())
		{
			debugInfo = "!IsPointerOverGameObject";
			return false;
		}
		Vector3 mousePosition = Input.mousePosition;
		PointerEventData eventData = new PointerEventData(current)
		{
			position = mousePosition
		};
		current.RaycastAll(eventData, raycastHits);
		if (raycastHits.Count == 0)
		{
			debugInfo = "No raycastHits";
			return false;
		}
		foreach (RaycastResult raycastHit in raycastHits)
		{
			if (raycastHit.gameObject.layer == Layers.UI)
			{
				UITooltipProvider componentInParent = raycastHit.gameObject.GetComponentInParent<UITooltipProvider>();
				tooltipInfo = ((componentInParent == null) ? TooltipInfo.Empty : componentInParent.TooltipInfo);
				debugInfo = "IsMouseOverUI: UI layer object: " + raycastHit.gameObject.name;
				return true;
			}
			if (!(raycastHit.gameObject.transform.parent == current.transform))
			{
				continue;
			}
			PanelRaycaster component = raycastHit.gameObject.GetComponent<PanelRaycaster>();
			if (component != null)
			{
				IPanel panel = component.panel;
				Vector2 point = new Vector2(mousePosition.x, (float)Screen.height - mousePosition.y);
				VisualElement visualElement = panel.Pick(point);
				if (visualElement != null)
				{
					debugInfo = "Over VisualElement: \"" + visualElement.name + "\"";
					return true;
				}
			}
		}
		return false;
	}

	public static void RegisterEscapeHandler(EscapeHandler handler, Func<bool> action)
	{
		_escapeHandlers[handler] = action;
	}

	public static void UnregisterEscapeHandler(EscapeHandler handler)
	{
		_escapeHandlers.Remove(handler);
	}

	private void HandleEscape()
	{
		EscapeHandler[] array = new EscapeHandler[3]
		{
			EscapeHandler.Transient,
			EscapeHandler.QuickSearch,
			EscapeHandler.Pause
		};
		foreach (EscapeHandler key in array)
		{
			if (_escapeHandlers.TryGetValue(key, out var value) && value())
			{
				return;
			}
		}
		Debug.Log("Escape unhandled.");
	}

	private static void PushCar()
	{
		ObjectPicker objectPicker = ObjectPicker.Shared;
		if (objectPicker == null)
		{
			Log.Warning("Missing ObjectPicker.");
			return;
		}
		if (!(objectPicker.HoveringOver is CarPickable carPickable))
		{
			Log.Warning("Couldn't find car to push; hovering over: {hoveringOver}", objectPicker.HoveringOver);
			return;
		}
		CameraSelector cameraSelector = CameraSelector.shared;
		if (!cameraSelector.CurrentCameraIsFirstPerson)
		{
			Toast.Present("Only available in first person.");
			return;
		}
		PlayerController character = cameraSelector.character;
		if (!character.IsOnGround)
		{
			Debug.Log($"Can't push from here: IsStableOnGround = {character.character.motor.GroundingStatus.IsStableOnGround}, AttachedRigidbody = {character.character.motor.AttachedRigidbody}");
			Toast.Present("Must be on ground.");
			return;
		}
		if (objectPicker.DistanceToTarget > 5f)
		{
			Toast.Present("Too far away.");
			return;
		}
		Car car = carPickable.car;
		if (car.IsDerailed)
		{
			Log.Debug("Push {car} (rerail)", car);
			StateManager.ApplyLocal(new Rerail(new string[1] { car.id }, 0.26f));
			return;
		}
		Log.Debug("Push {car}", car);
		TrainController trainController = TrainController.Shared;
		Vector3 b = WorldTransformer.WorldToGame(Camera.main.transform.position);
		float num = Vector3.Distance(trainController.graph.GetPosition(car.WheelBoundsF), b);
		float num2 = Vector3.Distance(trainController.graph.GetPosition(car.WheelBoundsR), b);
		int direction = ((num < num2) ? 1 : (-1));
		StateManager.ApplyLocal(new ManualMoveCar(car.id, direction));
	}

	private static void RecallSelection()
	{
		TrainController trainController = TrainController.Shared;
		bool flag = trainController.SelectRecall();
		Car selectedCar = trainController.SelectedCar;
		string text = ((!flag) ? "Nothing to recall" : ("Selected " + selectedCar.DisplayName));
		Toast.Present(text, ToastPosition.Bottom);
	}

	private static void CycleSelection()
	{
		TrainController tc = TrainController.Shared;
		List<Car> list = (from id in PlayerPropertiesManager.Shared.MyProperties.FavoriteEngineIds
			select tc.CarForId(id) into car2
			where car2 != null
			orderby car2.SortName
			select car2).ToList();
		if (list.Count == 0)
		{
			Toast.Present("No favorites. Mark favorites in Engine Roster.", ToastPosition.Bottom);
			return;
		}
		Car selectedCar = tc.SelectedCar;
		int num = list.IndexOf(selectedCar);
		Car car = ((num < 0) ? list[0] : list[(num + 1) % list.Count]);
		if (!(car == selectedCar))
		{
			tc.SelectedCar = car;
			Toast.Present("Selected " + car.DisplayName, ToastPosition.Bottom);
		}
	}

	private void AutoEngineerSetWaypoint()
	{
		BaseLocomotive selectedLocomotive = TrainController.Shared.SelectedLocomotive;
		if (!(selectedLocomotive == null))
		{
			AutoEngineerWaypointControls autoEngineerWaypointControls = AutoEngineerWaypointControls.Shared;
			if (autoEngineerWaypointControls != null)
			{
				autoEngineerWaypointControls.PresentWaypointPicker(selectedLocomotive);
			}
		}
	}
}
