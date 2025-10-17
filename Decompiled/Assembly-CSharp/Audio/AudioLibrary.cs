using System;
using System.Collections.Generic;
using UnityEngine;

namespace Audio;

[CreateAssetMenu(fileName = "Audio Library", menuName = "Railroader/Audio/Audio Library", order = 0)]
public class AudioLibrary : ScriptableObject
{
	[Serializable]
	public class Entry
	{
		public string name;

		public AudioClip clip;

		[Range(0f, 1f)]
		public float volumeMultiplier = 1f;
	}

	[SerializeField]
	public List<Entry> entries = new List<Entry>();

	public bool TryGetEntry(string entryName, out Entry entry)
	{
		int num = entries.FindIndex((Entry e) => e.name == entryName);
		if (num < 0)
		{
			entry = new Entry();
			return false;
		}
		entry = entries[num];
		return true;
	}
}
