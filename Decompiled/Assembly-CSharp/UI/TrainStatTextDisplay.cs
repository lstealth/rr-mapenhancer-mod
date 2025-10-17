using TMPro;
using UnityEngine;

namespace UI;

public class TrainStatTextDisplay : TrainStatDisplay
{
	public TMP_Text[] speedometerTexts;

	public TMP_Text bp;

	public TMP_Text bc;

	protected override void SetGauges(float milesPerHour, float mainResPsi, float eqResPsi, float brakeCylPsi, float brakePipePsi)
	{
		TMP_Text[] array = speedometerTexts;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetText("{0:0.0}", milesPerHour);
		}
		bp.SetText("{0}", Mathf.RoundToInt(brakePipePsi));
		bc.SetText("{0}", Mathf.RoundToInt(brakeCylPsi));
	}
}
