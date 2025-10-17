using UnityEngine;

namespace Avatar;

[RequireComponent(typeof(Animator))]
public class AvatarAnimator : MonoBehaviour
{
	private Animator _animator;

	private static readonly int AnimIdVelocityX = Animator.StringToHash("velocityX");

	private static readonly int AnimIdVelocityZ = Animator.StringToHash("velocityZ");

	private static readonly int AnimIdSit = Animator.StringToHash("sit");

	private static readonly int AnimIdJump = Animator.StringToHash("jump");

	private static readonly int AnimIdLadder = Animator.StringToHash("ladder");

	private Vector3 _lookAtPosition = Vector3.zero;

	private AvatarPose _pose;

	private void Awake()
	{
		_animator = GetComponent<Animator>();
	}

	private void OnEnable()
	{
		string text = _pose switch
		{
			AvatarPose.Ladder => "SideOfCar", 
			AvatarPose.Sit => "Sit", 
			_ => null, 
		};
		if (text != null)
		{
			_animator.Play(text);
		}
	}

	private void OnAnimatorIK(int layerIndex)
	{
		if (_lookAtPosition != Vector3.zero)
		{
			_animator.SetLookAtWeight(0.8f);
			_animator.SetLookAtPosition(_lookAtPosition);
		}
		else
		{
			_animator.SetLookAtWeight(0f);
		}
	}

	public void SetVelocity(Vector3 v, Vector3 lookAtPosition)
	{
		_animator.SetFloat(AnimIdVelocityX, v.x);
		_animator.SetFloat(AnimIdVelocityZ, v.z);
		_lookAtPosition = lookAtPosition;
	}

	public void SetPose(AvatarPose pose)
	{
		_pose = pose;
		_animator.SetBool(AnimIdSit, pose == AvatarPose.Sit);
		_animator.SetBool(AnimIdJump, pose == AvatarPose.Jump);
		_animator.SetBool(AnimIdLadder, pose == AvatarPose.Ladder);
	}
}
