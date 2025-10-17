using UnityEngine;

namespace WorldStreamer2;

public class PlayerTeleport : MonoBehaviour
{
	[Tooltip("If teleport/respawn should initiate loading screen, drag and drop here your \"Loading Screen UI\" object  from your scene hierarchy or an object that contain \"UI Loading Streamer\" script.")]
	public UILoadingStreamer uiLoadingStreamer;

	[Tooltip("If teleport/respawn should initiate player move to safe position.")]
	public PlayerMover playerMover;

	[Tooltip("List of streamers. Drag and drop here all your streamer objects from scene hierarchy.")]
	public Streamer[] streamers;

	[Tooltip("Object that should be moved during respawn/teleport process. It must be the same as object that streamer fallows during streaming process.")]
	public Transform player;

	[Tooltip("If you use Floating Point fix system drag and drop world mover prefab from your scene hierarchy.")]
	public WorldMover worldMover;

	public void Teleport(bool showLoadingScreen)
	{
		if (player != null)
		{
			player.position = base.transform.position + ((worldMover == null) ? Vector3.zero : worldMover.currentMove);
			player.rotation = base.transform.rotation;
			Streamer[] array = streamers;
			foreach (Streamer obj in array)
			{
				obj.showLoadingScreen = showLoadingScreen;
				obj.CheckPositionTiles();
			}
			if (uiLoadingStreamer != null)
			{
				uiLoadingStreamer.Show();
			}
			if (playerMover != null)
			{
				playerMover.MovePlayer();
			}
		}
		else if (streamers[0] != null && streamers[0].player != null)
		{
			player = streamers[0].player;
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = new Color(0.4f, 0.7f, 1f, 0.5f);
		Gizmos.DrawSphere(base.transform.position, 1f);
	}
}
