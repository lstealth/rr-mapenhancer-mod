using UnityEngine;

namespace Character;

public struct PlayerCharacterInputs
{
	public float MoveAxisForward;

	public float MoveAxisRight;

	public Quaternion CameraRotation;

	public bool JumpDown;

	public bool CrouchDown;

	public bool CrouchUp;

	public float RotateAxisY;

	public Lean Lean;

	public bool Run;
}
