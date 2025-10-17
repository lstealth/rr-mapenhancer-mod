using System;
using Avatar;
using Game;
using Game.Messages;
using Game.State;
using Helpers;
using UnityEngine;

namespace Network.Client;

public class RemotePlayer : MonoBehaviour, IPlayer
{
	public PlayerId playerId;

	public string playerName;

	private RemoteAvatar _avatar;

	public string Name => playerName;

	public bool IsRemote => true;

	public PlayerId PlayerId => playerId;

	public Vector3 GamePosition => _avatar.transform.position.WorldToGame();

	private void OnDestroy()
	{
		if (_avatar != null && AvatarManager.Instance != null)
		{
			AvatarManager.Instance.RemoveAvatar(_avatar);
		}
		_avatar = null;
	}

	public RemoteAvatar AddUpdateAvatar(AvatarDescriptor avatarDescriptor)
	{
		if (_avatar == null)
		{
			AvatarManager instance = AvatarManager.Instance;
			_avatar = instance.AddRemote(playerId, playerName);
		}
		_avatar.avatar.Customization.Configure(avatarDescriptor);
		return _avatar;
	}

	public void UpdateAvatarPosition(Vector3 position, Vector3 forward, Vector3 look, Vector3 velocity, string relativeToCarId, AvatarPose pose, long tick)
	{
		_avatar.AddPosition(tick, position, forward, look, velocity, relativeToCarId, pose);
	}

	public void ConfigureAvatar(Vector3 position, string relativeToCarId, Vector3 forward, Vector3 look, Snapshot.CharacterCustomization customization)
	{
		AvatarDescriptor avatarDescriptor = AvatarDescriptor.From(customization);
		AddUpdateAvatar(avatarDescriptor).AddPosition(StateManager.Now, position, forward, look, Vector3.zero, relativeToCarId, AvatarPose.Stand);
	}

	public RemotePlayer CheckedRemotePlayer()
	{
		if (this == null)
		{
			throw new Exception($"RemotePlayer is null playerId={playerId}");
		}
		return this;
	}

	public override string ToString()
	{
		return Name;
	}
}
