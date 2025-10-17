using Network.Client;
using UnityEngine;

namespace Game;

public interface IPlayer
{
	string Name { get; }

	bool IsRemote { get; }

	PlayerId PlayerId { get; }

	Vector3 GamePosition { get; }

	RemotePlayer CheckedRemotePlayer();
}
