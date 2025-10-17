using UnityEngine;

namespace Audio;

public interface IAudioSource
{
	AudioClip clip { get; set; }

	float volume { get; set; }

	AudioRolloffMode rolloffMode { get; set; }

	float minDistance { set; }

	float maxDistance { set; }

	float time { get; set; }

	float pitch { get; set; }

	float dopplerLevel { get; set; }

	float spatialBlend { get; set; }

	bool loop { get; set; }

	AudioController.Group outputAudioMixerGroup { get; }

	bool isPlaying { get; }

	AnimationCurve rolloffCurve { get; set; }

	void Play();

	void Stop();

	void PlayScheduled(double time);

	void SetHighPassCutoff(float cutoff);

	void SetLowPassCutoff(float cutoff);
}
