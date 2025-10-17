using Game;
using UI;
using UI.CompanyWindow;
using UnityEngine;

namespace Avatar;

public class AvatarPickable : MonoBehaviour, IPickable
{
	public float MaxPickDistance => 500f;

	public int Priority => 0;

	public TooltipInfo TooltipInfo { get; set; }

	public PickableActivationFilter ActivationFilter => PickableActivationFilter.PrimaryOnly;

	public PlayerId PlayerId { get; set; }

	public void Activate(PickableActivateEvent evt)
	{
		if (GameInput.IsControlDown && PlayerId.IsValid)
		{
			CompanyWindow.Shared.ShowPlayer(PlayerId.String);
		}
	}

	public void Deactivate()
	{
	}
}
