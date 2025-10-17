using System.Collections.Generic;
using UnityEngine;

namespace Track.Signals;

public class CTCPanelColumnPrefab : MonoBehaviour
{
	public List<CTCPanelLamp> switchLamps = new List<CTCPanelLamp>(2);

	public CTCPanelKnob switchKnob;

	public List<CTCPanelLamp> signalLamps = new List<CTCPanelLamp>(3);

	public CTCPanelKnob signalKnob;

	public CTCPanelButton codeButton;

	public CTCPanelGroup group;
}
