using UnityEngine;
using UnityEngine.InputSystem;

namespace UI;

[DefaultExecutionOrder(-100)]
public class PressInput : MonoBehaviour
{
	private class ActivateState
	{
		public readonly InputAction Action;

		public readonly float LongPressThreshold;

		public float PressedTime = -1f;

		public Vector2 MouseStart;

		public Recognized Recognized;

		public bool ShortPressedThisFrame;

		public bool LongPressStartedThisFrame;

		public bool UnpressedThisFrame;

		public bool IsRecognized => Recognized != Recognized.Pending;

		public ActivateState(float longPressThreshold, InputAction action)
		{
			Action = action;
			LongPressThreshold = longPressThreshold;
		}

		public void Reset()
		{
			PressedTime = -1f;
			Recognized = Recognized.Pending;
		}
	}

	private enum Recognized
	{
		Pending,
		Short,
		Long
	}

	[SerializeField]
	private GameInput gameInput;

	private const float MovedThreshold = 6f;

	private ActivateState _primaryState;

	private ActivateState _secondaryState;

	public bool PrimaryPressStartedThisFrame
	{
		get
		{
			if (!_primaryState.ShortPressedThisFrame)
			{
				return _primaryState.LongPressStartedThisFrame;
			}
			return true;
		}
	}

	public bool PrimaryPressEndedThisFrame => _primaryState.UnpressedThisFrame;

	public bool SecondaryPressedThisFrame => _secondaryState.ShortPressedThisFrame;

	public bool SecondaryLongPressBeganThisFrame => _secondaryState.LongPressStartedThisFrame;

	public bool SecondaryLongPressEndedThisFrame => _secondaryState.UnpressedThisFrame;

	private void Awake()
	{
		InputActionAsset inputActions = gameInput.inputActions;
		_primaryState = new ActivateState(0.2f, inputActions["Game/ActivatePrimary"]);
		_secondaryState = new ActivateState(0.2f, inputActions["Game/ActivateSecondary"]);
	}

	private void Update()
	{
		float unscaledTime = Time.unscaledTime;
		UpdateState(_primaryState, unscaledTime);
		UpdateState(_secondaryState, unscaledTime);
	}

	private static void UpdateState(ActivateState state, float now)
	{
		state.ShortPressedThisFrame = false;
		state.LongPressStartedThisFrame = false;
		state.UnpressedThisFrame = false;
		Vector3 mousePosition = Input.mousePosition;
		InputAction action = state.Action;
		if (action.WasPressedThisFrame())
		{
			state.PressedTime = now;
			state.MouseStart = mousePosition;
		}
		if (state.PressedTime < 0f)
		{
			return;
		}
		float num = now - state.PressedTime;
		bool flag = Vector3.Distance(mousePosition, state.MouseStart) > 6f;
		if (action.WasReleasedThisFrame())
		{
			if (num <= state.LongPressThreshold && !state.IsRecognized)
			{
				state.ShortPressedThisFrame = true;
				state.Recognized = Recognized.Short;
			}
			state.UnpressedThisFrame = true;
			state.Reset();
		}
		else if ((num > state.LongPressThreshold || flag) && !state.IsRecognized)
		{
			state.LongPressStartedThisFrame = true;
			state.Recognized = Recognized.Long;
		}
	}
}
