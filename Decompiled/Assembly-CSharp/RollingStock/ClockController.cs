using Game;
using UnityEngine;

namespace RollingStock;

public class ClockController : MonoBehaviour
{
	[SerializeField]
	private GaugeBehaviour hours;

	[SerializeField]
	private GaugeBehaviour minutes;

	[SerializeField]
	private GaugeBehaviour seconds;

	private void Update()
	{
		float num = TimeWeather.Now.Hours;
		float value = ((num > 12f) ? (num - 12f) : num);
		float num2 = Mathf.Repeat(num, 1f) * 60f;
		float value2 = Mathf.Repeat(num2 * 60f, 60f);
		hours.Value = value;
		minutes.Value = num2;
		seconds.Value = value2;
	}
}
