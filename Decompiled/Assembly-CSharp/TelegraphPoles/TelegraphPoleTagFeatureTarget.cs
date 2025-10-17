using UnityEngine;

namespace TelegraphPoles;

public class TelegraphPoleTagFeatureTarget : MonoBehaviour
{
	[SerializeField]
	private TelegraphPoleManager manager;

	[SerializeField]
	private int poleTag;

	private void OnEnable()
	{
		if (Application.isPlaying)
		{
			manager.SetTagEnabled(poleTag, enable: true);
		}
	}

	private void OnDisable()
	{
		if (Application.isPlaying)
		{
			manager.SetTagEnabled(poleTag, enable: false);
		}
	}
}
