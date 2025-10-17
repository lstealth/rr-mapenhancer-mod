using System.Text;
using Serilog;
using UnityEngine;

namespace RollingStock.Steam;

public class ChuffFilter : MonoBehaviour
{
	private struct Parameters
	{
		public float EngineSpeed;

		public float EngineCutoff;

		public float EngineThrottle;
	}

	private AudioHighPassFilter _highPassFilter;

	private AudioLowPassFilter _lowPassFilter;

	private AudioReverbFilter _reverbFilter;

	[SerializeField]
	private AnimationCurve curve;

	[Header("Engine Parameters")]
	[Range(0.1f, 20f)]
	[SerializeField]
	public float engineSpeed = 5f;

	[Range(0f, 1f)]
	[SerializeField]
	public float engineCutoff = 1f;

	[Range(0f, 1f)]
	[SerializeField]
	public float engineSize = 1f;

	[Range(0f, 1f)]
	[SerializeField]
	public float engineThrottle = 1f;

	[Range(0f, 1f)]
	[SerializeField]
	public float engineNormalizedTE = 1f;

	[SerializeField]
	private ChuffFilterProfile profile;

	[SerializeField]
	private bool enableUpdateCurve = true;

	private bool _running;

	private int _sampleRate;

	private float _sampleTime;

	private float _lowPassModulationTime;

	private float _lowPassBase;

	private float _highPassBase;

	private float _engineVolume;

	private Parameters _parameters;

	private readonly object _parameterLock = new object();

	private static bool _loggedNaNCoeff;

	private void Awake()
	{
		_highPassFilter = GetComponent<AudioHighPassFilter>();
		_lowPassFilter = GetComponent<AudioLowPassFilter>();
		_reverbFilter = GetComponent<AudioReverbFilter>();
		curve = new AnimationCurve(profile.amplitudeCurve.keys);
	}

	private void OnEnable()
	{
		_sampleRate = AudioSettings.outputSampleRate;
		_running = true;
	}

	private void OnDisable()
	{
		_running = false;
	}

	private void Update()
	{
		_lowPassFilter.cutoffFrequency = _lowPassBase + profile.lowPassModulation * Mathf.Lerp(-1f, 1f, Mathf.PerlinNoise(_lowPassModulationTime, 0f)) + profile.lowPassOffsetForSpeed.Evaluate(engineSpeed);
		_lowPassModulationTime += Time.deltaTime * engineSpeed * profile.lowPassModulationSpeed;
		_highPassFilter.cutoffFrequency = _highPassBase + profile.highPassOffsetForSpeed.Evaluate(engineSpeed);
		lock (_parameterLock)
		{
			_parameters = new Parameters
			{
				EngineSpeed = engineSpeed,
				EngineThrottle = engineThrottle,
				EngineCutoff = engineCutoff
			};
		}
	}

	private void OnAudioFilterRead(float[] data, int channels)
	{
		if (!_running)
		{
			return;
		}
		Parameters parameters;
		lock (_parameterLock)
		{
			parameters = _parameters;
		}
		float num = parameters.EngineSpeed;
		float num2 = parameters.EngineCutoff;
		float num3 = parameters.EngineThrottle;
		if (num < 0.001f)
		{
			for (int i = 0; i < data.Length; i++)
			{
				data[i] = 0f;
			}
			return;
		}
		if (enableUpdateCurve)
		{
			float time = num2;
			Keyframe key = curve.keys[1];
			key.time = profile.attackTime.Evaluate(time);
			curve.MoveKey(1, key);
		}
		int num4 = data.Length / channels;
		float chuffSpeedMult = Mathf.Lerp(profile.fullCutoffMultiplier, 1f, num2);
		float num5 = chuffSpeedMult / num;
		if (num5 > profile.maximumChuffDuration)
		{
			chuffSpeedMult *= num5 / profile.maximumChuffDuration;
		}
		for (int j = 0; j < num4; j++)
		{
			float num6 = Evaluate(0f) + Evaluate(0.25f) + Evaluate(0.5f) + Evaluate(0.75f);
			num6 *= profile.throttleToVolumeCurve.Evaluate(num3);
			num6 *= _engineVolume;
			if (float.IsNaN(num6))
			{
				if (!_loggedNaNCoeff)
				{
					StringBuilder stringBuilder = new StringBuilder();
					stringBuilder.Append($"({num}, {chuffSpeedMult}, {_sampleTime}) ");
					stringBuilder.Append($"({Evaluate(0f)} + {Evaluate(0.25f)} + {Evaluate(0.5f)} + {Evaluate(0.75f)})");
					stringBuilder.Append($" * {profile.throttleToVolumeCurve.Evaluate(num3)} ({num3})");
					stringBuilder.Append($" * {_engineVolume}");
					Log.Error("ChuffFilter: Found NaN coeff: {debugInfo}", stringBuilder.ToString());
					_loggedNaNCoeff = true;
				}
				num6 = 0f;
			}
			for (int k = 0; k < channels; k++)
			{
				int num7 = j * channels + k;
				float num8 = data[num7] * num6;
				data[num7] = num8;
			}
			_sampleTime += num * 1f / (float)_sampleRate;
		}
		_sampleTime = Mathf.Repeat(_sampleTime, 1f);
		float Evaluate(float phaseOffset)
		{
			return curve.Evaluate(Mathf.Repeat(_sampleTime + phaseOffset, 1f) * chuffSpeedMult);
		}
	}

	[ContextMenu("Update for Engine Characteristics")]
	public void UpdateForEngineCharacteristics()
	{
		_lowPassBase = profile.sizeToLowPassCutoff.Evaluate(engineSize);
		_highPassBase = profile.sizeToHighPassCutoff.Evaluate(engineSize);
		_engineVolume = profile.sizeToVolumeCurve.Evaluate(engineSize);
	}

	public float GetNextChuffDelay()
	{
		if (engineSpeed == 0f)
		{
			return float.MaxValue;
		}
		return (0.25f - Mathf.Repeat(_sampleTime, 0.25f)) / engineSpeed;
	}
}
