using UnityEngine;
using UnityEngine.UI;

namespace Audio;

[RequireComponent(typeof(RollingPlayer))]
public class RollingPlayerTester : MonoBehaviour
{
	private RollingPlayer _player;

	public Text text;

	private void Awake()
	{
		_player = GetComponent<RollingPlayer>();
	}

	public void SliderValue(float value)
	{
		_player.overrideVelocity = value / 2.23694f;
		text.text = $"{value:F2} mph, {value / 2.23694f:F2} m/s";
	}
}
