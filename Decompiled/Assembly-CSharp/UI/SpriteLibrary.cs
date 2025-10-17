using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI;

[CreateAssetMenu(fileName = "Sprite Library", menuName = "Railroader/Sprite Library", order = 0)]
public class SpriteLibrary : ScriptableObject
{
	[Serializable]
	public struct Entry
	{
		public SpriteName name;

		public Sprite sprite;
	}

	public List<Entry> entries;

	public static SpriteLibrary Shared
	{
		get
		{
			SpriteLibrarySentinel instance = SpriteLibrarySentinel.Instance;
			if (instance == null)
			{
				return null;
			}
			return instance.library;
		}
	}
}
