using UnityEngine;
using UnityEngine.UI;

namespace WorldStreamer2;

public class PlayerPositionUI : MonoBehaviour
{
	[Tooltip("Object that should be moved during respawn/teleport process. It must be the same as object that streamer fallows during streaming process.")]
	public Transform player;

	[Tooltip("If you use Floating Point fix system drag and drop world mover prefab from your scene hierarchy.")]
	public WorldMover worldMover;

	public Text text;

	public void Start()
	{
		if (player == null)
		{
			Debug.LogError("Player is not connected to Position Gizmo");
		}
		text.text = "Player position: Player is not connected to Position Gizmo";
	}

	public void Update()
	{
		if (player != null)
		{
			if (worldMover != null)
			{
				Text obj = text;
				string obj2 = player.transform.position.ToString();
				Vector3 playerPositionMovedLooped = worldMover.playerPositionMovedLooped;
				obj.text = "Player position: " + obj2 + "\nPlayer real position: " + playerPositionMovedLooped.ToString();
			}
			else
			{
				text.text = "Player position: " + player.transform.position.ToString() + "\nPlayer real position: Not Connected to World Mover";
			}
		}
	}
}
