using System.Collections.Generic;
using UnityEngine;

namespace WorldStreamer2;

public class ColliderStreamerManager : MonoBehaviour
{
	[Tooltip("Object that will start loading process after it hits the collider.")]
	public Transform player;

	[Tooltip("Collider Streamer Manager will wait for player spawn and fill it automatically")]
	public bool spawnedPlayer;

	[HideInInspector]
	public string playerTag = "Player";

	public static string COLLIDERSTREAMERMANAGERTAG = "ColliderStreamerManager";

	public List<ColliderStreamer> colliderStreamers;

	public void AddColliderStreamer(ColliderStreamer colliderStreamer)
	{
		colliderStreamers.Add(colliderStreamer);
	}

	public void AddColliderScene(ColliderScene colliderScene)
	{
		foreach (ColliderStreamer colliderStreamer in colliderStreamers)
		{
			if (colliderStreamer != null && colliderStreamer.sceneName == colliderScene.sceneName)
			{
				colliderStreamer.SetSceneGameObject(colliderScene.gameObject);
				break;
			}
		}
	}

	public void Update()
	{
		if (spawnedPlayer && player == null && !string.IsNullOrEmpty(playerTag))
		{
			GameObject gameObject = GameObject.FindGameObjectWithTag(playerTag);
			if (gameObject != null)
			{
				player = gameObject.transform;
			}
		}
	}
}
