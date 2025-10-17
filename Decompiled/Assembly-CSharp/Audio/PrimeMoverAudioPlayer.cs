using System;
using System.Collections;
using System.Text;
using Model;
using UnityEngine;

namespace Audio;

public class PrimeMoverAudioPlayer : MonoBehaviour, IPrimeMoverAudioPlayer
{
	public PrimeMoverAudioProfile profile;

	private IAudioSource _loopSourceA;

	private IAudioSource _loopSourceB;

	private Coroutine _coroutine;

	private float _notchFloat;

	private BaseLocomotive _locomotive;

	public Action<float> NormalizedExhaustOutputEvent { get; set; }

	public int Notch { get; set; }

	public string DebugText => new StringBuilder().ToString();

	private void OnEnable()
	{
		_locomotive = GetComponentInParent<BaseLocomotive>();
		_locomotive.OnHasFuelDidChange += UpdatePlayStop;
		UpdatePlayStop();
	}

	private void OnDisable()
	{
		StopPlaying();
		_locomotive.OnHasFuelDidChange -= UpdatePlayStop;
	}

	private void UpdatePlayStop()
	{
		bool hasFuel = _locomotive.HasFuel;
		bool flag = _coroutine != null;
		if (hasFuel != flag)
		{
			if (hasFuel)
			{
				StartPlaying();
			}
			else
			{
				StopPlaying();
			}
		}
	}

	private void StartPlaying()
	{
		AudioController.Group mixerGroup = AudioController.Group.Locomotive;
		AudioDistance audioDistance = AudioDistance.Distant;
		int priority = 10;
		_loopSourceA = CreateSource();
		_loopSourceB = CreateSource();
		_coroutine = StartCoroutine(PrimeMoverCoroutine());
		IAudioSource CreateSource()
		{
			IAudioSource audioSource = VirtualAudioSourcePool.Checkout("PrimeMover", null, loop: true, mixerGroup, priority, base.transform, audioDistance);
			audioSource.volume = 0f;
			audioSource.rolloffMode = AudioRolloffMode.Custom;
			audioSource.minDistance = 50f;
			audioSource.maxDistance = 500f;
			audioSource.rolloffCurve = profile.rolloffCurve;
			return audioSource;
		}
	}

	private void StopPlaying()
	{
		if (_coroutine != null)
		{
			StopCoroutine(_coroutine);
		}
		_coroutine = null;
		VirtualAudioSourcePool.Return(_loopSourceA);
		VirtualAudioSourcePool.Return(_loopSourceB);
		_loopSourceA = null;
		_loopSourceB = null;
	}

	private IEnumerator PrimeMoverCoroutine()
	{
		SetExhaust(Notch);
		PlayNext(profile.notchLoops[Notch], loop: true, 1f, 0f);
		Swap();
		int lastNotch = Notch;
		while (true)
		{
			if (lastNotch == Notch)
			{
				yield return null;
				continue;
			}
			int nextNotch = Notch;
			int delta = nextNotch - lastNotch;
			SetExhaust(nextNotch);
			float loopCrossfadeDuration = 0.25f;
			if (Mathf.Abs(delta) == 1)
			{
				AudioClip transition = ((delta > 0) ? profile.transitionsUp[lastNotch] : profile.transitionsDown[lastNotch - 1]);
				if (transition != null)
				{
					PlayNext(transition, loop: false, 0f, 0f);
					yield return CrossfadeThenSwap(0.25f);
					yield return WaitForSecondsOrUntilNotchChanges(transition.length - 0.5f, nextNotch);
				}
				else
				{
					loopCrossfadeDuration = 1f;
				}
				lastNotch = nextNotch;
			}
			else
			{
				AudioClip transition = ((delta > 0) ? profile.transitionsUp[lastNotch] : profile.transitionsDown[lastNotch - 1]);
				if (transition != null)
				{
					PlayNext(transition, loop: false, 0f, 0f);
					yield return CrossfadeThenSwap(0.25f);
					yield return WaitForSecondsOrUntilNotchChanges(transition.length * 0.25f - 0.5f, nextNotch);
				}
				lastNotch = nextNotch;
				AudioClip audioClip = ((delta > 0) ? profile.transitionsUp[nextNotch - 1] : profile.transitionsDown[nextNotch]);
				if (audioClip != null)
				{
					PlayNext(audioClip, loop: false, 0f, 0.25f);
					float remainingTransitionToDuration = audioClip.length * 0.75f;
					yield return CrossfadeThenSwap(1f);
					yield return WaitForSecondsOrUntilNotchChanges(remainingTransitionToDuration - 1.25f, nextNotch);
				}
				else
				{
					loopCrossfadeDuration = 1f;
				}
			}
			PlayNext(profile.notchLoops[nextNotch], loop: true, 0f, 0f);
			yield return CrossfadeThenSwap(loopCrossfadeDuration);
		}
	}

	private void PlayNext(AudioClip clip, bool loop, float volume, float normalizedTime)
	{
		IAudioSource loopSourceB = _loopSourceB;
		loopSourceB.clip = clip;
		loopSourceB.loop = loop;
		loopSourceB.volume = volume * profile.volume;
		loopSourceB.time = clip.length * normalizedTime;
		loopSourceB.Play();
	}

	private IEnumerator WaitForSecondsOrUntilNotchChanges(float duration, int currentNotch)
	{
		double stopAfter = AudioSettings.dspTime + (double)duration;
		while (Notch == currentNotch && AudioSettings.dspTime < stopAfter)
		{
			yield return null;
		}
	}

	private void Swap()
	{
		IAudioSource loopSourceB = _loopSourceB;
		IAudioSource loopSourceA = _loopSourceA;
		_loopSourceA = loopSourceB;
		_loopSourceB = loopSourceA;
	}

	private IEnumerator CrossfadeThenSwap(float duration)
	{
		yield return CrossfadeHelper.Crossfade(_loopSourceA, _loopSourceB, duration, profile.volume);
		Swap();
	}

	private void SetExhaust(int notch)
	{
		float obj = Mathf.InverseLerp(0f, 8f, notch);
		NormalizedExhaustOutputEvent?.Invoke(obj);
	}
}
