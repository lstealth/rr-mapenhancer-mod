using UnityEngine;

namespace Audio;

public static class AudioSourceExtensions
{
	public static void ApplyBase3DSettings(this AudioSource source)
	{
		source.spatialBlend = 1f;
		source.rolloffMode = AudioRolloffMode.Logarithmic;
		source.minDistance = 20f;
		source.maxDistance = 100f;
	}
}
