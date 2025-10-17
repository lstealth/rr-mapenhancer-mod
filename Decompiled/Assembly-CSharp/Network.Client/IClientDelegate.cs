using System.Collections.Generic;
using Game;
using Game.AccessControl;
using Game.Messages;
using Network.Messages;
using Network.Server;

namespace Network.Client;

public interface IClientDelegate
{
	void ClientStatusDidChange(Network.Server.ClientStatus statusStatus, PlayerId playerId, AccessLevel accessLevel);

	void ClientErrorOccurred(string displayText);

	void ClientDidDisconnect(int endReason);

	void ClientDidReceiveGameMessage(PlayerId sender, IGameMessage message);

	void ClientDidReceivePlayerList(Dictionary<string, Snapshot.Player> players);

	void ClientDidReceiveSnapshot(Snapshot snapshot);

	void ClientDidReceiveAlert(Alert alert);

	void ClientDidReceiveTimeSync(TimeSync timeSync);

	void ClientDidReceivePasswordPrompt();
}
