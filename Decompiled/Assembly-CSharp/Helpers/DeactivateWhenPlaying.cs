using UnityEngine;

namespace Helpers;

public class DeactivateWhenPlaying : MonoBehaviour
{
	private void Awake()
	{
		if (Application.isPlaying)
		{
			base.gameObject.SetActive(value: false);
		}
	}
}
