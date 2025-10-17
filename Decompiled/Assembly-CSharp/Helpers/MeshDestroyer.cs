using UnityEngine;

namespace Helpers;

public class MeshDestroyer : MonoBehaviour
{
	public Mesh mesh;

	private void OnDestroy()
	{
		Object.Destroy(mesh);
	}
}
