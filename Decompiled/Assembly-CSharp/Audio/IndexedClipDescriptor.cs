using System;
using UnityEngine;

namespace Audio;

[CreateAssetMenu(fileName = "IndexedClip", menuName = "Train Game/Indexed Clip Descriptor")]
public class IndexedClipDescriptor : ScriptableObject
{
	[Serializable]
	public struct Index
	{
		public float time;
	}

	public AudioClip clip;

	public Index[] indexes;
}
