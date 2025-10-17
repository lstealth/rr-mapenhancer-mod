using System;
using System.Runtime.InteropServices;
using Game.State;
using Helpers;
using Network.Client;
using UnityEngine;

namespace Game;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct LocalPlayer : IPlayer
{
	public string Name => Preferences.MultiplayerClientUsername;

	public bool IsRemote => false;

	public PlayerId PlayerId => PlayersManager.PlayerId;

	public Vector3 GamePosition => CameraSelector.shared.localAvatar.character.GroundPosition.WorldToGame();

	public RemotePlayer CheckedRemotePlayer()
	{
		throw new Exception("LocalPlayer instance is not RemotePlayer");
	}

	public override string ToString()
	{
		return Name;
	}
}
