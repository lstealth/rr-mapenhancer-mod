using Avatar;
using Game;
using Game.State;
using UnityEngine;

namespace Character;

public class LocalAvatar : MonoBehaviour
{
	public PlayerController character;

	public Vector3 lanternOffset = new Vector3(-0.34f, 0.4f, 0.15f);

	private AvatarPrefab _avatar;

	public AvatarPose Pose { get; set; }

	public bool LanternEnabled
	{
		get
		{
			return _avatar.lantern.activeSelf;
		}
		set
		{
			_avatar.lantern.SetActive(value);
		}
	}

	private static bool showAvatar => !CameraSelector.shared.CurrentCameraIsFirstPerson;

	private void OnDestroy()
	{
		if (!(_avatar == null))
		{
			AvatarManager.Instance.RemoveAvatar(_avatar);
			_avatar = null;
		}
	}

	private void FixedUpdate()
	{
		if (!(_avatar == null))
		{
			MotionSnapshot motionSnapshot = character.character.GetMotionSnapshot();
			_avatar.Rigidbody.MovePosition(motionSnapshot.Position);
			_avatar.Rigidbody.MoveRotation(motionSnapshot.BodyRotation.normalized);
			_avatar.Animator.SetPose(Pose);
		}
	}

	public void CurrentCameraDidChange()
	{
		if (showAvatar && _avatar == null)
		{
			SetupAvatarIfNeeded();
		}
		if (_avatar != null)
		{
			_avatar.SetAvatarVisible(showAvatar);
		}
	}

	public void SetAvatarCustomization(AvatarDescriptor avatarDescriptor)
	{
		Preferences.AvatarDescriptor = avatarDescriptor;
		SetupAvatarIfNeeded();
		_avatar.Customization.Configure(avatarDescriptor);
		_avatar.SetAvatarVisible(showAvatar);
	}

	private void SetupAvatarIfNeeded()
	{
		if (!(_avatar != null))
		{
			_avatar = AvatarManager.Instance.AddAvatar(Preferences.AvatarDescriptor, showMapIcon: false, PlayersManager.PlayerId, "You");
		}
	}

	public void SetSeat(bool seat, bool ladder)
	{
		if (seat)
		{
			Pose = AvatarPose.Sit;
		}
		else if (ladder)
		{
			Pose = AvatarPose.Ladder;
		}
		else
		{
			Pose = AvatarPose.Stand;
		}
	}
}
