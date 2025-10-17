using UnityEngine;

namespace WorldStreamer2;

public class ObjectToMove : MonoBehaviour
{
	private void Start()
	{
		GameObject gameObject = GameObject.FindGameObjectWithTag(WorldMover.WORLDMOVERTAG);
		if (gameObject != null)
		{
			gameObject.GetComponent<WorldMover>().AddObjectToMove(base.transform);
		}
	}
}
