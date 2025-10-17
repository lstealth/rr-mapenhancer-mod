using UnityEngine;

namespace Track.Signals;

[SelectionBase]
public class CTCPanelButton : MonoBehaviour, IPickable
{
	public string id;

	private SignalStorage _storage;

	public float MaxPickDistance => 5f;

	public int Priority => 0;

	public TooltipInfo TooltipInfo => new TooltipInfo("Code Button", null);

	public PickableActivationFilter ActivationFilter => PickableActivationFilter.PrimaryOnly;

	private void OnEnable()
	{
		_storage = GetComponentInParent<SignalStorage>();
	}

	private void OnDisable()
	{
	}

	public void Activate(PickableActivateEvent evt)
	{
		_storage.SetButton(id, value: true);
	}

	public void Deactivate()
	{
	}
}
