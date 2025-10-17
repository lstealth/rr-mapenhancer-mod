using UnityEngine;

namespace WorldStreamer2;

public class ColliderScene : MonoBehaviour
{
	public string sceneName;

	private void Start()
	{
		GameObject.FindGameObjectWithTag(ColliderStreamerManager.COLLIDERSTREAMERMANAGERTAG).GetComponent<ColliderStreamerManager>().AddColliderScene(this);
	}
}
