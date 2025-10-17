using UnityEngine;

namespace Audio;

public class ParametricAudioPlayer : MonoBehaviour
{
	public ParametricAudioComposition composition;

	[Range(0f, 1f)]
	public float parameter;

	[Range(0f, 1f)]
	public float volume = 1f;

	public int priority = 128;

	private AudioSource[] _sources;

	private void Update()
	{
		if (composition == null)
		{
			return;
		}
		ParametricAudioComposition.Track[] tracks = composition.tracks;
		if (_sources == null || tracks.Length != _sources.Length)
		{
			if (_sources != null)
			{
				AudioSource[] sources = _sources;
				for (int i = 0; i < sources.Length; i++)
				{
					Object.Destroy(sources[i].gameObject);
				}
			}
			_sources = new AudioSource[tracks.Length];
			for (int j = 0; j < tracks.Length; j++)
			{
				GameObject obj = new GameObject();
				obj.hideFlags = HideFlags.DontSave;
				obj.name = tracks[j].clip.name;
				obj.transform.SetParent(base.transform, worldPositionStays: false);
				AudioSource audioSource = obj.AddComponent<AudioSource>();
				audioSource.ApplyBase3DSettings();
				audioSource.clip = tracks[j].clip;
				audioSource.loop = true;
				audioSource.volume = 0f;
				audioSource.priority = priority;
				_sources[j] = audioSource;
			}
		}
		parameter = Mathf.Clamp01(parameter);
		volume = Mathf.Clamp01(volume);
		for (int k = 0; k < tracks.Length; k++)
		{
			ParametricAudioComposition.Track track = tracks[k];
			AudioSource audioSource2 = _sources[k];
			audioSource2.volume = volume * track.volumeCurve.Evaluate(parameter);
			audioSource2.pitch = track.pitchCurve.Evaluate(parameter);
			if (!audioSource2.isPlaying)
			{
				audioSource2.Play();
			}
		}
	}
}
