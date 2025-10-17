using System;
using UnityEngine;

namespace Audio;

[CreateAssetMenu(fileName = "Horn Profile", menuName = "Train Game/Audio/Horn Profile", order = 0)]
public class HornProfile : ScriptableObject
{
	[Serializable]
	public class Layer
	{
		public AudioClip clip;

		public AnimationCurve volumeCurve;
	}

	public Layer[] layers;

	public AudioClip leadIn;
}
