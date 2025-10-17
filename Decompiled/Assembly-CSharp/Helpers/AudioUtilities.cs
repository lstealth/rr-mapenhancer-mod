using System;
using UnityEngine;

namespace Helpers;

public static class AudioUtilities
{
	public static AudioClip[] Split(this AudioClip clip, float[] times, int fadeEndSamples)
	{
		int channels = clip.channels;
		float[] array = new float[clip.samples * channels];
		clip.GetData(array, 0);
		AudioClip[] array2 = new AudioClip[times.Length + 1];
		int num = 0;
		for (int i = 0; i <= times.Length; i++)
		{
			int num2 = ((i == times.Length) ? clip.samples : ((int)(times[i] * (float)clip.frequency)));
			AudioClip audioClip = AudioClip.Create($"{i}", num2 - num, channels, clip.frequency, stream: false);
			float[] array3 = new float[(num2 - num) * channels];
			Array.Copy(array, num * channels, array3, 0, array3.Length);
			FadeEnds(array3, channels, fadeEndSamples);
			audioClip.SetData(array3, 0);
			array2[i] = audioClip;
			num = num2;
		}
		return array2;
	}

	private static void FadeEnds(float[] samples, int channels, int count)
	{
		if (samples.Length < count * 2)
		{
			return;
		}
		for (int i = 0; i < count; i++)
		{
			float num = Mathf.Lerp(0f, 1f, (float)i / (float)count);
			for (int j = 0; j < channels; j++)
			{
				samples[i * channels + j] *= num;
			}
		}
		for (int k = 0; k < count; k++)
		{
			float num2 = Mathf.Lerp(0f, 1f, (float)k / (float)count);
			int num3 = samples.Length / channels - 1 - k;
			for (int l = 0; l < channels; l++)
			{
				samples[num3 * channels + l] *= num2;
			}
		}
	}

	public static AudioClip Loopify(this AudioClip clip)
	{
		int num = ((clip.samples % 2 == 0) ? clip.samples : (clip.samples - 1));
		int channels = clip.channels;
		float[] array = new float[clip.samples * channels];
		clip.GetData(array, 0);
		int num2 = 4096;
		int sourceIndex = num / 2;
		int num3 = (num / 2 - num2) * 2 + num2;
		float[] array2 = new float[num3 * channels];
		int num4 = num / 2 - num2;
		Array.Copy(array, sourceIndex, array2, 0, num4);
		Array.Copy(array, num2, array2, num3 - num4, num4);
		for (int i = 0; i < num2; i++)
		{
			int num5 = i;
			int num6 = num - num2 + i;
			array2[num4 + i] = Mathf.Lerp(array[num6], array[num5], (float)i / (float)num2);
		}
		AudioClip audioClip = AudioClip.Create(clip.name + "-Loopified", num3, channels, clip.frequency, stream: false);
		audioClip.SetData(array2, 0);
		return audioClip;
	}

	public static void Crossfade(out float a, out float b, float t)
	{
		float t2 = 1f - Mathf.Pow(t, 2f);
		float t3 = 1f - Mathf.Pow(1f - t, 2f);
		a = Mathf.Lerp(0f, 1f, t2);
		b = Mathf.Lerp(0f, 1f, t3);
	}
}
