using UnityEngine;

namespace UI;

public class SpriteLibrarySentinel : MonoBehaviour
{
	public static SpriteLibrarySentinel Instance;

	public SpriteLibrary library;

	private void Awake()
	{
		Instance = this;
	}

	private void OnDestroy()
	{
		Instance = null;
	}
}
