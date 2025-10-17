using UnityEngine.Audio;

namespace Audio;

public static class AudioMixerExtensions
{
	public static AudioMixerGroup Group(this AudioMixer mixer, AudioController.Group group)
	{
		string path = group.Path;
		return mixer.FindMatchingGroups(path)[0];
	}
}
