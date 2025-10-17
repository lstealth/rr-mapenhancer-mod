using UnityEngine;

namespace WorldStreamer2;

public class SceneSplitManager : MonoBehaviour
{
	public string sceneName;

	public Color color;

	public Vector3 position;

	[HideInInspector]
	public Vector3 size = new Vector3(10f, 10f, 10f);

	[HideInInspector]
	public Vector3Int wsPosition = new Vector3Int(10, 10, 10);

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = color;
		Gizmos.DrawWireCube(position + size * 0.5f, size);
	}
}
