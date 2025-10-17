using Game.Messages;
using Game.State;
using Track;
using UnityEngine;

namespace UI.Map;

public class MapSwitchStand : MonoBehaviour, IMapClickable
{
	public TrackNode node;

	public void Click()
	{
		StateManager.ApplyLocal(new RequestSetSwitch(node.id, !node.isThrown));
	}
}
