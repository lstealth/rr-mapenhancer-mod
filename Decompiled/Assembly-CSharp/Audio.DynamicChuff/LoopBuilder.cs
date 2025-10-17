using System;
using System.IO;
using Audio.Filters;
using UnityEngine;

namespace Audio.DynamicChuff;

internal class LoopBuilder
{
	private readonly float[][] _sourceSamples;

	private readonly int _sourceNumSamples;

	private readonly int _outputFrequency;

	private const int chuffsPerRevolution = 4;

	private SmbPitchShifter _pitchShifter = new SmbPitchShifter();

	private readonly BiQuadFilter _highPassFilter = BiQuadFilter.HighPassFilter(48000f, 1000f, 1f);

	private readonly BiQuadFilter _lowPassFilter = BiQuadFilter.LowPassFilter(48000f, 1000f, 1f);

	public int Frequency => _outputFrequency;

	public LoopBuilder(AudioClip[] sourceClips)
	{
		if (sourceClips.Length < 4)
		{
			throw new ArgumentException();
		}
		for (int i = 1; i < sourceClips.Length; i++)
		{
			AudioClip audioClip = sourceClips[0];
			AudioClip audioClip2 = sourceClips[i];
			if (audioClip.samples != audioClip2.samples)
			{
				throw new ArgumentException($"samples must match {i}: {audioClip.samples} vs {audioClip2.samples}");
			}
			if (audioClip.channels != audioClip2.channels)
			{
				throw new ArgumentException($"channels must match {i}: {audioClip.channels} vs {audioClip2.channels}");
			}
			if (audioClip.frequency != audioClip2.frequency)
			{
				throw new ArgumentException($"frequency must match {i}: {audioClip.frequency} vs {audioClip2.frequency}");
			}
		}
		if (sourceClips[0].channels != 1)
		{
			throw new ArgumentException("only mono clips are supported", "sourceClips");
		}
		_sourceSamples = new float[sourceClips.Length][];
		_outputFrequency = sourceClips[0].frequency;
		_sourceNumSamples = sourceClips[0].samples;
		for (int j = 0; j < sourceClips.Length; j++)
		{
			_sourceSamples[j] = new float[_sourceNumSamples];
			sourceClips[j].GetData(_sourceSamples[j], 0);
		}
	}

	public AudioClip[] BuildSingles(float absVelocity, float driverCircumference, float chuffLengthNormalized, float attack, float time, float highPassFrequency, float lowPassFrequency, out float chuffDuration, float maxChuffDuration)
	{
		float b = driverCircumference / ((float)_sourceNumSamples / (float)_outputFrequency);
		absVelocity = Mathf.Max(absVelocity, b);
		float num = driverCircumference / absVelocity;
		chuffDuration = num / 4f;
		float num2 = Mathf.Clamp(Mathf.Clamp01(chuffLengthNormalized) * num, 0f, maxChuffDuration);
		int num3 = Mathf.FloorToInt(num2 * (float)_outputFrequency);
		float[] array = new float[num3];
		AudioClip[] array2 = new AudioClip[4];
		int num4 = Mathf.Min(_sourceSamples[0].Length - 1, Mathf.FloorToInt((float)_outputFrequency * num2));
		for (int i = 0; i < array2.Length; i++)
		{
			Array.Clear(array, 0, array.Length);
			float num5 = 1f;
			ResetFilters();
			UpdateFilters(highPassFrequency, lowPassFrequency, time + (float)i * chuffDuration);
			for (int j = 0; j < num3; j++)
			{
				float parameter = Mathf.InverseLerp(0f, num4, j);
				float sample = _sourceSamples[i][j];
				sample = FilterSample(sample);
				sample *= num5 * Envelope(parameter, attack);
				array[j] = sample;
			}
			AudioClip audioClip = AudioClip.Create("ChuffSingle", num3, 1, _outputFrequency, stream: false);
			audioClip.SetData(array, 0);
			array2[i] = audioClip;
			WriteToFile($"Chuff{i}.pcm", array);
		}
		return array2;
	}

	public AudioClip BuildLoop(float absVelocity, float driverCircumference, float chuffLengthNormalized, float attack, out float chuffDuration)
	{
		int a = _sourceSamples[0].Length;
		float[][] sourceSamples = _sourceSamples;
		if (absVelocity < 0.1f)
		{
			throw new ArgumentException($"absVelocity is too low {absVelocity}", "absVelocity");
		}
		float num = driverCircumference / absVelocity;
		chuffDuration = num / 4f;
		int num2 = Mathf.Max(4, Mathf.RoundToInt(2f / num / 4f) * 4);
		float num3 = (float)num2 * num;
		int num4 = Mathf.FloorToInt(num * (float)_outputFrequency);
		int num5 = Mathf.FloorToInt(num3 * (float)_outputFrequency);
		a = Mathf.Min(a, num5);
		float[] array = new float[num5];
		float num6 = Mathf.Clamp01(chuffLengthNormalized) * num;
		int num7 = 4 * num2;
		int[] array2 = new int[num7];
		int[] array3 = new int[num7];
		int[] array4 = new int[num7];
		array2[0] = 0;
		array2[1] = Mathf.FloorToInt((float)num4 * 0.5f);
		array2[2] = Mathf.FloorToInt((float)num4 * 0.252f);
		array2[3] = Mathf.FloorToInt((float)num4 * 0.752f);
		for (int i = 4; i < num7; i++)
		{
			array2[i] = array2[i - 4] + num4;
		}
		Debug.Log($"durationPerRevolution {num:F2} -> {num2} rev, {num7} tracks, total loop duration {num3:F2}");
		int num8 = Mathf.Min(a - 1, Mathf.FloorToInt((float)_outputFrequency * num6));
		for (int j = 0; j < num7; j++)
		{
			int num9 = array2[j] + num8;
			array3[j] = num9 % num5;
			array4[j] = j % sourceSamples.Length;
		}
		for (int k = 0; k < num5; k++)
		{
			float num10 = 0f;
			for (int l = 0; l < num7; l++)
			{
				int num11 = array2[l];
				int num12 = array3[l];
				int num13;
				if (num11 <= num12)
				{
					if (k < num11 || k > num12)
					{
						continue;
					}
					num13 = k - num11;
				}
				else
				{
					if (k <= num11 && k > num12)
					{
						continue;
					}
					num13 = ((k < num11) ? (num5 - 1 - num11 + k) : (k - num11));
				}
				float num14 = ((l % 2 == 0) ? 0.9f : 1f);
				if (num13 < 0 || num13 >= a)
				{
					Debug.LogError($"{num13} < {a}, outputSampleIndex={k}, trackStart={num11}, trackEnd={num12}");
				}
				float parameter = Mathf.InverseLerp(0f, num8, num13);
				int num15 = array4[l];
				float num16 = sourceSamples[num15][num13];
				num16 *= num14 * Envelope(parameter, attack);
				num10 += num16;
			}
			array[k] = num10;
		}
		AudioClip audioClip = AudioClip.Create("ChuffLoopOld", num5, 1, _outputFrequency, stream: false);
		audioClip.SetData(array, 0);
		return audioClip;
	}

	public void BuildLoopBuffer(float[] thisBuffer, float[] nextBuffer, float time, int timeOffsetInSamples, int chuffOffset, float absVelocity, float driverCircumference, float chuffLengthNormalized, float attack, float highPassFreqCenter, float lowPassFreqCenter, out float chuffDuration, out int nextTimeOffsetInSamples, out int nextChuffOffset)
	{
		if (timeOffsetInSamples < 0)
		{
			throw new ArgumentException("Negative timeOffsetInSamples", "timeOffsetInSamples");
		}
		if (chuffOffset < 0)
		{
			throw new ArgumentException("Negative chuffOffset", "chuffOffset");
		}
		int num = thisBuffer.Length;
		int a = _sourceSamples[0].Length;
		if (absVelocity < 0.1f)
		{
			throw new ArgumentException($"absVelocity is too low {absVelocity}", "absVelocity");
		}
		float num2 = driverCircumference / absVelocity;
		chuffDuration = num2 / 4f;
		float num3 = (float)num / (float)_outputFrequency;
		if (timeOffsetInSamples > num)
		{
			throw new ArgumentException("timeOffsetInSamples > bufferLength", "timeOffsetInSamples");
		}
		int num4 = Mathf.FloorToInt(num2 * (float)_outputFrequency);
		a = Mathf.Min(a, num);
		float num5 = Mathf.Clamp01(chuffLengthNormalized) * num2;
		int i = 1;
		for (float num6 = (float)timeOffsetInSamples / (float)_outputFrequency; (float)i * chuffDuration + num6 - num3 < chuffDuration && (float)(i + 1) * chuffDuration + num6 < 2f * num3; i++)
		{
		}
		int[] array = new int[i];
		int[] array2 = new int[i];
		int[] array3 = new int[i];
		for (int j = 0; j < i; j++)
		{
			int[] array4 = array;
			int num7 = j;
			array4[num7] = j switch
			{
				0 => timeOffsetInSamples, 
				1 => timeOffsetInSamples + Mathf.FloorToInt((float)num4 * 0.248f), 
				2 => timeOffsetInSamples + Mathf.FloorToInt((float)num4 * 0.5f), 
				3 => timeOffsetInSamples + Mathf.FloorToInt((float)num4 * 0.748f), 
				_ => array[j - 4] + num4, 
			};
		}
		int num8 = Mathf.Min(a - 1, Mathf.FloorToInt((float)_outputFrequency * num5));
		for (int k = 0; k < i; k++)
		{
			int num9 = array[k] + num8;
			array2[k] = num9;
			array3[k] = k % _sourceSamples.Length;
		}
		for (int l = 0; l < num; l++)
		{
			if (l % num / 10 == 0)
			{
				UpdateFilters(highPassFreqCenter, lowPassFreqCenter, time + (float)l / (float)_outputFrequency);
			}
			float num10 = 0f;
			for (int m = 0; m < i; m++)
			{
				float num11 = CalculateSampleValue(m, l, array, array2, _sourceSamples, num8, array3, attack, num);
				num10 += num11;
			}
			float sample = thisBuffer[l] + num10;
			sample = FilterSample(sample);
			thisBuffer[l] = sample;
		}
		Array.Clear(nextBuffer, 0, nextBuffer.Length);
		int num12 = array2[^1];
		int num13 = array[^1];
		for (int n = 0; n < num && n < num12 - num; n++)
		{
			float num14 = 0f;
			for (int num15 = 0; num15 < i; num15++)
			{
				float num16 = CalculateSampleValue(num15, num + n, array, array2, _sourceSamples, num8, array3, attack, num);
				num14 += num16;
			}
			nextBuffer[n] = num14;
		}
		nextTimeOffsetInSamples = num13 + num4 / 4;
		nextTimeOffsetInSamples -= num;
		nextChuffOffset = (i + 1) % 4;
	}

	private float FilterSample(float sample)
	{
		sample = _highPassFilter.Transform(sample);
		sample = _lowPassFilter.Transform(sample);
		return sample;
	}

	private void UpdateFilters(float highPassFreqCenter, float lowPassFreqCenter, float time)
	{
		float t = Mathf.PerlinNoise(time * 0.5f, 0.5f);
		float cutoffFrequency = Mathf.Clamp(Mathf.Lerp(highPassFreqCenter - 50f, highPassFreqCenter + 50f, t), 1f, 10000f);
		_highPassFilter.SetHighPassFilter(_outputFrequency, cutoffFrequency, 1f);
		float cutoffFrequency2 = Mathf.Clamp(Mathf.Lerp(lowPassFreqCenter - 500f, lowPassFreqCenter + 500f, t), 1f, 20000f);
		_lowPassFilter.SetLowPassFilter(_outputFrequency, cutoffFrequency2, 1f);
	}

	private void ResetFilters()
	{
		_highPassFilter.Reset();
		_lowPassFilter.Reset();
	}

	private static float CalculateSampleValue(int chuffIndex, int outputIndex, int[] chuffStarts, int[] chuffEnds, float[][] sourceSamples, float chuffLengthSamples, int[] chuffSources, float attack, int bufferLength)
	{
		int num = sourceSamples[0].Length;
		int num2 = chuffStarts[chuffIndex];
		int num3 = chuffEnds[chuffIndex];
		int num4;
		if (num2 <= num3)
		{
			if (outputIndex < num2 || outputIndex > num3)
			{
				return 0f;
			}
			num4 = outputIndex - num2;
		}
		else
		{
			if (outputIndex <= num2 && outputIndex > num3)
			{
				return 0f;
			}
			num4 = ((outputIndex < num2) ? (bufferLength - 1 - num2 + outputIndex) : (outputIndex - num2));
		}
		if (num4 < 0 || num4 >= num)
		{
			return 0f;
		}
		float num5 = ((chuffIndex % 2 == 0) ? 0.9f : 1f);
		if (num4 < 0 || num4 >= num)
		{
			Debug.LogError($"{num4} < {num}, outputSampleIndex={outputIndex}, chuffStart={num2}, chuffEnd={num3}");
		}
		float parameter = Mathf.InverseLerp(0f, chuffLengthSamples, num4);
		int num6 = chuffSources[chuffIndex];
		return sourceSamples[num6][num4] * (num5 * Envelope(parameter, attack));
	}

	private static float Envelope(float parameter, float center)
	{
		if (parameter <= 0f || parameter >= 1f)
		{
			return 0f;
		}
		if ((parameter < center && center > 0f) || center >= 1f)
		{
			float num = QuadEaseInOut(parameter / center);
			return num * num;
		}
		return QuadEaseInOut((0f - (parameter - 1f)) / (1f - center));
		static float QuadEaseInOut(float t)
		{
			float num2 = t * t;
			return num2 / (2f * (num2 - t) + 1f);
		}
	}

	public static void WriteToFile(string filename, float[] output)
	{
		using FileStream output2 = File.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), filename));
		using BinaryWriter binaryWriter = new BinaryWriter(output2);
		foreach (float value in output)
		{
			binaryWriter.Write(value);
		}
	}

	public static void AppendToFile(string filename, float[] output)
	{
		using FileStream fileStream = File.OpenWrite(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), filename));
		fileStream.Seek(0L, SeekOrigin.End);
		using BinaryWriter binaryWriter = new BinaryWriter(fileStream);
		foreach (float value in output)
		{
			binaryWriter.Write(value);
		}
	}
}
