using UnityEngine;

namespace Audio.Filters;

public class EqFilter : MonoBehaviour
{
	private readonly BiQuadFilter[] _filters = new BiQuadFilter[2]
	{
		new BiQuadFilter(),
		new BiQuadFilter()
	};

	[SerializeField]
	public float centerFrequency = 1000f;

	[SerializeField]
	public float bandwidth = 100f;

	[SerializeField]
	public float dbGain;

	private void OnEnable()
	{
		ResetBiQuadFilter();
	}

	private void OnValidate()
	{
		ResetBiQuadFilter();
	}

	private void OnAudioFilterRead(float[] data, int channels)
	{
		int num = data.Length;
		for (int i = 0; i < num; i += channels)
		{
			for (int j = 0; j < channels; j++)
			{
				float inSample = data[i + j];
				inSample = _filters[j].Transform(inSample);
				data[i + j] = inSample;
			}
		}
	}

	private void ResetBiQuadFilter()
	{
		_filters[0].SetPeakingEq(AudioSettings.outputSampleRate, centerFrequency, bandwidth, dbGain);
		_filters[1].SetPeakingEq(AudioSettings.outputSampleRate, centerFrequency, bandwidth, dbGain);
	}
}
