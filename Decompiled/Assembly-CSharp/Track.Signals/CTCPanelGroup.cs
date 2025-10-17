using System.Collections.Generic;
using Game;

namespace Track.Signals;

public class CTCPanelGroup : GameBehaviour
{
	public string interlockingId;

	public CTCPanelKnob switchKnob;

	public CTCPanelKnob signalKnob;

	public List<TrackNode> switches;

	private bool _switchKnobExists;

	private bool _signalKnobExists;

	private void Awake()
	{
		_switchKnobExists = switchKnob.gameObject.activeSelf;
		_signalKnobExists = signalKnob.gameObject.activeSelf;
	}

	protected override void OnEnableWithProperties()
	{
		SystemMode systemMode = CTCPanelController.Shared.SystemMode;
		UpdateForMode(systemMode);
	}

	public void UpdateForMode(SystemMode mode)
	{
		bool flag = mode == SystemMode.CTC;
		bool showSwitchItems = _switchKnobExists && flag;
		bool showSignalItems = _signalKnobExists && flag;
		switchKnob.gameObject.SetActive(showSwitchItems);
		signalKnob.gameObject.SetActive(showSignalItems);
		CTCPanelColumnPrefab component = GetComponent<CTCPanelColumnPrefab>();
		if (component != null)
		{
			component.switchLamps.ForEach(delegate(CTCPanelLamp lamp)
			{
				lamp.gameObject.SetActive(showSwitchItems);
			});
			component.signalLamps.ForEach(delegate(CTCPanelLamp lamp)
			{
				lamp.gameObject.SetActive(showSignalItems);
			});
			component.codeButton.gameObject.SetActive(flag);
		}
	}
}
