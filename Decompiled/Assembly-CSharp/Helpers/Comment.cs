using UnityEngine;

namespace Helpers;

public class Comment : MonoBehaviour
{
	public string text;

	private void OnDrawGizmos()
	{
		Gizmos.DrawIcon(base.transform.position, "Comment.png", allowScaling: true);
	}
}
