using Avatar;
using Game.Messages;
using Game.State;
using Helpers;
using Network;
using UnityEngine;

namespace Character;

public class CharacterPositionTransmitter : MonoBehaviour
{
	private PlayerController _firstPersonController;

	private UpdateCharacterPosition _lastSentUpdateCharacterPosition;

	private long _lastSentTick;

	private void Awake()
	{
		_firstPersonController = GetComponent<PlayerController>();
	}

	public void SendIfConnected(AvatarPose pose)
	{
		if (Multiplayer.Client == null || !Multiplayer.IsClientActive)
		{
			return;
		}
		long now = StateManager.Now;
		if (_lastSentTick != 0L && Mathf.Abs(_lastSentTick - now) > 20000f)
		{
			Debug.LogWarning($"Resetting _lastSendTick: {now} - {_lastSentTick} = {now - _lastSentTick}");
			_lastSentTick = 0L;
		}
		if (_lastSentTick + 100 <= now)
		{
			bool force = _lastSentTick + 2000 <= now;
			var (motionSnapshot, car) = _firstPersonController.GetRelativePositionRotation();
			if (car == null)
			{
				motionSnapshot.Position = WorldTransformer.WorldToGame(motionSnapshot.Position);
			}
			SendIfNeeded(motionSnapshot.Position, motionSnapshot.BodyRotation * Vector3.forward, motionSnapshot.LookRotation * Vector3.forward, motionSnapshot.Velocity, pose, car?.id, force);
		}
	}

	private void SendIfNeeded(Vector3 position, Vector3 forward, Vector3 look, Vector3 velocity, AvatarPose pose, string relativeToCarId, bool force)
	{
		long now = StateManager.Now;
		CharacterPosition position2 = new CharacterPosition(position, relativeToCarId, forward, look);
		CharacterPosition position3 = _lastSentUpdateCharacterPosition.Position;
		UpdateCharacterPosition updateCharacterPosition = new UpdateCharacterPosition(position2, velocity, (CharacterPose)pose, now);
		float magnitude = (position2.Position - position3.Position).magnitude;
		float magnitude2 = (position2.Forward - position3.Forward).magnitude;
		float magnitude3 = (position2.Look - position3.Look).magnitude;
		float magnitude4 = (updateCharacterPosition.Velocity - _lastSentUpdateCharacterPosition.Velocity).magnitude;
		bool flag = position2.RelativeToCarId != position3.RelativeToCarId;
		bool flag2 = updateCharacterPosition.Pose != _lastSentUpdateCharacterPosition.Pose;
		bool flag3 = magnitude > 0.05f || magnitude2 > 0.1f || magnitude3 > 0.1f || magnitude4 > 0.01f || flag || flag2;
		if (force || flag3)
		{
			Multiplayer.Client.Send(updateCharacterPosition);
			_lastSentUpdateCharacterPosition = updateCharacterPosition;
			_lastSentTick = now;
		}
	}
}
