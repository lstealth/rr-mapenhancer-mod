using UnityEngine;

namespace Track.Signals;

public class CTCSignalPickable : MonoBehaviour, IPickable
{
	private CTCSignal _signal;

	public float MaxPickDistance => 200f;

	public int Priority => 0;

	public TooltipInfo TooltipInfo => BuildTooltipInfo();

	public PickableActivationFilter ActivationFilter => PickableActivationFilter.PrimaryOnly;

	public void Activate(PickableActivateEvent evt)
	{
	}

	public void Deactivate()
	{
	}

	private TooltipInfo BuildTooltipInfo()
	{
		if (_signal == null)
		{
			_signal = GetComponentInParent<CTCSignal>();
		}
		string text;
		string text2;
		switch (_signal.CurrentAspect)
		{
		case SignalAspect.Stop:
			if (_signal.IsIntermediate)
			{
				text = "Stop and Proceed".ColorRed();
				text2 = "Stop and proceed at restricted speed.";
			}
			else
			{
				text = "Stop".ColorRed();
				text2 = "Stop and do not pass.";
			}
			break;
		case SignalAspect.Approach:
			text = "Approach".ColorYellow();
			text2 = "Proceed prepared to stop at next signal.";
			break;
		case SignalAspect.Clear:
			text = "Clear".ColorGreen();
			text2 = "Proceed.";
			break;
		case SignalAspect.DivergingApproach:
			text = "Diverging Approach".ColorYellow();
			text2 = "Proceed on diverging route prepared to stop at next signal.";
			break;
		case SignalAspect.DivergingClear:
			text = "Diverging Clear".ColorGreen();
			text2 = "Proceed on diverging route.";
			break;
		case SignalAspect.Restricting:
			text = "Restricting".ColorRed();
			text2 = "Proceed at restricted speed.";
			break;
		default:
			text = "Unknown Aspect";
			text2 = "";
			break;
		}
		return new TooltipInfo(_signal.DisplayName, text + "\n" + text2);
	}
}
