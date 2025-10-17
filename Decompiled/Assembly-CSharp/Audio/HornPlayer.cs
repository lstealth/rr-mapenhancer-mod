using System.Collections;
using UnityEngine;

namespace Audio;

public class HornPlayer : MonoBehaviour
{
	public HornProfile profile;

	public float value;

	private IAudioSource _sourceA;

	private IAudioSource _sourceB;

	private float _currentValue;

	private void OnEnable()
	{
		AudioController.Group mixerGroup = AudioController.Group.LocomotiveWhistle;
		AudioDistance audioDistance = AudioDistance.Distant;
		int priority = 10;
		_sourceA = CreateSource();
		_sourceB = CreateSource();
		_sourceA.clip = profile.layers[0].clip;
		_sourceB.clip = profile.layers[1].clip;
		StartCoroutine(Player());
		IAudioSource CreateSource()
		{
			IAudioSource audioSource = VirtualAudioSourcePool.Checkout("Horn", null, loop: true, mixerGroup, priority, base.transform, audioDistance);
			audioSource.volume = 0f;
			audioSource.minDistance = 50f;
			audioSource.maxDistance = 1000f;
			return audioSource;
		}
	}

	private void OnDisable()
	{
		VirtualAudioSourcePool.Return(_sourceA);
		VirtualAudioSourcePool.Return(_sourceB);
		_sourceA = null;
		_sourceB = null;
	}

	private IEnumerator Player()
	{
		while (true)
		{
			if (value < 0.001f)
			{
				yield return null;
				continue;
			}
			_sourceA.volume = 0f;
			_sourceB.volume = 0f;
			_sourceA.Play();
			_sourceB.Play();
			float flow = 0f;
			do
			{
				flow = Mathf.Lerp(flow, (value > 0.001f) ? 1 : 0, Time.deltaTime * 20f);
				_currentValue = Mathf.Lerp(_currentValue, value, Time.deltaTime * 5f);
				float num = flow;
				_sourceA.volume = num * profile.layers[0].volumeCurve.Evaluate(_currentValue);
				_sourceB.volume = num * profile.layers[1].volumeCurve.Evaluate(_currentValue);
				yield return null;
			}
			while (flow > 0.001f);
			_sourceA.Stop();
			_sourceB.Stop();
		}
	}
}
