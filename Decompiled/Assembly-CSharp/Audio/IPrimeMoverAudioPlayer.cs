using System;

namespace Audio;

public interface IPrimeMoverAudioPlayer
{
	int Notch { get; set; }

	Action<float> NormalizedExhaustOutputEvent { get; set; }
}
