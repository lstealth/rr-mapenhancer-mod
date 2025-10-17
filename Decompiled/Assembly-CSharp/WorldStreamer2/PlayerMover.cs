using UnityEngine;
using UnityEngine.Events;

namespace WorldStreamer2;

public class PlayerMover : MonoBehaviour
{
	[Tooltip("List of streamers objects that should affect loading screen. Drag and drop here all your streamer objects from scene hierarchy which should be used in loading screen.")]
	public Streamer[] streamers;

	[Space(10f)]
	[Tooltip("Drag and drop here, an object that system have to follow during streaming process.")]
	public Transform player;

	[Tooltip("The player safe position during loading.")]
	public Transform safePosition;

	[Space(10f)]
	public UnityEvent onDone;

	private GameObject temporaryObject;

	private float progress;

	private bool waitForPlayer;

	private bool playerMoved;

	private void Awake()
	{
		if (streamers.Length != 0)
		{
			if (!streamers[0].spawnedPlayer)
			{
				MovePlayer();
			}
			else
			{
				waitForPlayer = true;
			}
		}
	}

	private void Update()
	{
		if (waitForPlayer)
		{
			if (player == null && !string.IsNullOrEmpty(streamers[0].playerTag))
			{
				GameObject gameObject = GameObject.FindGameObjectWithTag(streamers[0].playerTag);
				if (gameObject != null)
				{
					player = gameObject.transform;
					MovePlayer();
					waitForPlayer = false;
				}
			}
		}
		else
		{
			if (playerMoved)
			{
				return;
			}
			if (streamers.Length != 0)
			{
				bool flag = true;
				progress = 0f;
				Streamer[] array = streamers;
				foreach (Streamer streamer in array)
				{
					progress += streamer.LoadingProgress / (float)streamers.Length;
					flag = flag && streamer.initialized;
				}
				if (flag && progress >= 1f)
				{
					if (onDone != null)
					{
						onDone.Invoke();
					}
					Done();
				}
			}
			else
			{
				Debug.Log("No streamer Attached");
			}
		}
	}

	public void Done()
	{
		player.position = temporaryObject.transform.position;
		player.rotation = temporaryObject.transform.rotation;
		Streamer[] array = streamers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].player = player;
		}
		Object.Destroy(temporaryObject);
		playerMoved = true;
		base.gameObject.SetActive(value: false);
	}

	public void MovePlayer()
	{
		temporaryObject = new GameObject("Temporary");
		temporaryObject.transform.position = player.position;
		temporaryObject.transform.rotation = player.rotation;
		Streamer[] array = streamers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].player = temporaryObject.transform;
		}
		Debug.Log(safePosition.position);
		player.position = safePosition.position;
		player.rotation = safePosition.rotation;
		base.gameObject.SetActive(value: true);
		playerMoved = false;
	}
}
