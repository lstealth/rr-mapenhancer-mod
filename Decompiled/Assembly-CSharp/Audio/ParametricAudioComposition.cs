using System;
using UnityEngine;

namespace Audio;

[CreateAssetMenu(fileName = "Composition", menuName = "Train Game/Parametric Audio Composition", order = 0)]
public class ParametricAudioComposition : ScriptableObject
{
	[Serializable]
	public class Track
	{
		public AudioClip clip;

		public AnimationCurve pitchCurve;

		public AnimationCurve volumeCurve;
	}

	public Track[] tracks;
}
