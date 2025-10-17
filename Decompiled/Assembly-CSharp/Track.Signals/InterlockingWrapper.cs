using System.Collections;
using KeyValue.Runtime;

namespace Track.Signals;

internal struct InterlockingWrapper
{
	public KeyValueObject KeyValueObject;

	public CTCPanelGroup[] PanelGroups;

	public void SetSwitch(SwitchSetting setting, int index = 0)
	{
		string key = PanelGroups[index].KeyForSwitchKnob();
		KeyValueObject[key] = Value.Int((int)setting);
	}

	public void SetSignal(SignalDirection direction)
	{
		string key = PanelGroups[0].KeyForSignalKnob();
		KeyValueObject[key] = Value.Int((int)direction);
	}

	public IEnumerator Code()
	{
		string key = PanelGroups[0].KeyForCodeButton();
		KeyValueObject[key] = Value.Bool(value: true);
		yield return null;
		KeyValueObject[key] = Value.Bool(value: false);
	}

	public SwitchSetting DisplayedSwitchSetting(int index = 0)
	{
		string key = PanelGroups[index].KeyForSwitchKnob();
		return (SwitchSetting)KeyValueObject[key].IntValue;
	}

	public void SetSwitchAll(SwitchSetting setting)
	{
		for (int i = 0; i < PanelGroups.Length; i++)
		{
			SetSwitch(setting, i);
		}
	}
}
