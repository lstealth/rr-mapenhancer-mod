using Game.Messages;
using Game.State;
using Track;
using UI;
using UI.Common;
using UI.ContextMenu;
using UnityEngine;

internal class SwitchStandClick : MonoBehaviour, IPickable
{
	public TrackNode node;

	public float MaxPickDistance => 200f;

	public int Priority => 0;

	public PickableActivationFilter ActivationFilter => PickableActivationFilter.Any;

	public TooltipInfo TooltipInfo => new TooltipInfo
	{
		Title = (node.IsCTCSwitch ? "CTC " : "") + "Switch " + (node.isThrown ? "Reversed".ColorRed() : "Normal".ColorGreen()),
		Text = TooltipText()
	};

	public void Activate(PickableActivateEvent evt)
	{
		switch (evt.Activation)
		{
		case PickableActivation.Primary:
			StateManager.ApplyLocal(GetActivateMessage());
			break;
		case PickableActivation.Secondary:
			ShowContextMenu();
			break;
		}
	}

	public void Deactivate()
	{
	}

	private string TooltipText()
	{
		if (!StateManager.CheckAuthorizedToSendMessage(GetActivateMessage()))
		{
			return "<sprite name=\"MouseNo\"> N/A";
		}
		string text = ((!node.IsCTCSwitch) ? "<sprite name=\"MouseLeft\"> Throw" : (node.IsCTCSwitchUnlocked ? "<sprite name=\"MouseLeft\"> Throw (Unlocked)" : "<sprite name=\"MouseNo\"> CTC Controlled (Locked)"));
		if (GameInput.IsShiftDown && AuditManager.Shared.TryGetSwitchText(node.id, out var report))
		{
			text = text + "\n\n" + report;
		}
		return text;
	}

	private RequestSetSwitch GetActivateMessage()
	{
		return new RequestSetSwitch(node.id, !node.isThrown);
	}

	private void ShowContextMenu()
	{
		bool isCTCSwitchUnlocked = node.IsCTCSwitchUnlocked;
		RequestSetSwitchUnlocked toggleLocked = new RequestSetSwitchUnlocked(node.id, !isCTCSwitchUnlocked);
		if (!StateManager.CheckAuthorizedToSendMessage(toggleLocked) || !node.IsCTCSwitch)
		{
			Toast.Present("No context options available.");
			return;
		}
		UI.ContextMenu.ContextMenu shared = UI.ContextMenu.ContextMenu.Shared;
		if (UI.ContextMenu.ContextMenu.IsShown)
		{
			shared.Hide();
		}
		shared.Clear();
		shared.AddButton(ContextMenuQuadrant.Brakes, isCTCSwitchUnlocked ? "Lock Switch" : "Unlock Switch", SpriteName.Select, delegate
		{
			StateManager.ApplyLocal(toggleLocked);
		});
		shared.Show("CTC Switch");
	}
}
