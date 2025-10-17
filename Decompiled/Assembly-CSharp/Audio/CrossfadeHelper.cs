using System.Collections;
using UnityEngine;

namespace Audio;

public static class CrossfadeHelper
{
	public static IEnumerator FadeOutStop(this IAudioSource audioSource, float duration, float? pitch = null)
	{
		float elapsed = 0f;
		float fromVolume0 = audioSource.volume;
		float fromPitch = audioSource.pitch;
		float toPitch = pitch ?? fromPitch;
		for (; elapsed < duration; elapsed += Time.deltaTime)
		{
			float num = Mathf.InverseLerp(0f, duration, elapsed);
			CalculateCrossfade(num, out var _, out var v2);
			audioSource.volume = fromVolume0 * v2;
			audioSource.pitch = Mathf.Lerp(fromPitch, toPitch, num);
			yield return null;
		}
		audioSource.volume = 0f;
		audioSource.Stop();
	}

	public static IEnumerator FadeIn(this IAudioSource audioSource, float duration, float fromVolume, float toVolume, float fromPitch, float toPitch)
	{
		float elapsed = 0f;
		audioSource.Play();
		for (; elapsed < duration; elapsed += Time.deltaTime)
		{
			float t = Mathf.InverseLerp(0f, duration, elapsed);
			audioSource.volume = Mathf.Lerp(fromVolume, toVolume, t);
			audioSource.pitch = Mathf.Lerp(fromPitch, toPitch, t);
			yield return null;
		}
		audioSource.volume = toVolume;
		audioSource.pitch = toPitch;
	}

	public static IEnumerator Crossfade(IAudioSource from, IAudioSource to, float duration, float targetVolume = 1f)
	{
		double t0 = AudioSettings.dspTime;
		double stopTime = t0 + (double)duration;
		float fromVolume0 = from.volume;
		while (AudioSettings.dspTime < stopTime)
		{
			float value = (float)(AudioSettings.dspTime - t0);
			CalculateCrossfade(Mathf.InverseLerp(0f, duration, value), out var v, out var v2);
			from.volume = fromVolume0 * v2;
			to.volume = v * targetVolume;
			yield return null;
		}
		to.volume = targetVolume;
		from.Stop();
	}

	private static void CalculateCrossfade(float p, out float v0, out float v1)
	{
		float num = Mathf.Lerp(-1f, 1f, p);
		v0 = Mathf.Sqrt(0.5f * (1f + num));
		v1 = Mathf.Sqrt(0.5f * (1f - num));
	}
}
