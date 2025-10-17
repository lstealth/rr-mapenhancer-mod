using UI;
using UnityEngine;

namespace Audio.Sandbox;

[RequireComponent(typeof(WhistlePlayer))]
public class WhistleDemoInput : MonoBehaviour
{
	private WhistlePlayer _whistle;

	private void Awake()
	{
		_whistle = GetComponent<WhistlePlayer>();
	}

	private void Update()
	{
		_whistle.parameter = GameInput.shared.InputHorn;
	}
}
