using Game.Messages;
using Game.State;
using UnityEngine;

namespace Game;

public class FlarePickable : MonoBehaviour, IPickable
{
	private PlayerId _placedBy;

	public string FlareId { get; private set; }

	public float MaxPickDistance => 100f;

	public int Priority => 0;

	public TooltipInfo TooltipInfo
	{
		get
		{
			IPlayer player = StateManager.Shared.PlayersManager.PlayerForId(_placedBy);
			string title = ((player == null) ? "Fusee" : (player.IsRemote ? (player.Name + "'s Fusee") : "Your Fusee"));
			return new TooltipInfo(title, "Click to Extinguish");
		}
	}

	public PickableActivationFilter ActivationFilter => PickableActivationFilter.PrimaryOnly;

	public void Activate(PickableActivateEvent evt)
	{
		StateManager.ApplyLocal(new FlareRemove(FlareId));
	}

	public void Deactivate()
	{
	}

	public void Configure(string flareId, PlayerId placedBy)
	{
		FlareId = flareId;
		_placedBy = placedBy;
	}
}
