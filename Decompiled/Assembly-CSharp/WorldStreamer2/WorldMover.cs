using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldStreamer2;

public class WorldMover : MonoBehaviour
{
	public static string WORLDMOVERTAG = "WorldMover";

	[Tooltip("Frequency distance of world position restarting, distance in is grid elements.")]
	public float xTileRange = 2f;

	[Tooltip("Frequency distance of world position restarting, distance in is grid elements.")]
	public float yTileRange = 2f;

	[Tooltip("Frequency distance of world position restarting, distance in is grid elements.")]
	public float zTileRange = 2f;

	[Tooltip("Time from reaching frequency distance for world mover to restart world position.")]
	public float waitForRestart = 10f;

	[HideInInspector]
	public float xCurrentTile;

	[HideInInspector]
	public float yCurrentTile;

	[HideInInspector]
	public float zCurrentTile;

	[Tooltip("Drag and drop here, your _Streamer_Major prefab from scene hierarchy.")]
	public Streamer streamerMajor;

	[Tooltip("Drag and drop here, your all _Streamer_Minors prefabs from scene hierarchy.")]
	public Streamer[] streamerMinors;

	[Tooltip("Differences between real  and restarted player position. Useful in AI and network communications.")]
	public Vector3 currentMove = Vector3.zero;

	[HideInInspector]
	public List<Transform> objectsToMove = new List<Transform>();

	[Tooltip("Debug value used for client-server communication it's position without floating point fix and looping")]
	public Vector3 playerPositionMovedLooped;

	private Vector3 worldSize;

	private bool waitForMover;

	public void Start()
	{
		streamerMajor.worldMover = this;
		List<Streamer> list = new List<Streamer>();
		list.AddRange(streamerMinors);
		list.Remove(streamerMajor);
		streamerMinors = list.ToArray();
		worldSize = new Vector3(streamerMajor.sceneCollectionManagers[0].xSize * (streamerMajor.sceneCollectionManagers[0].xLimitsy - streamerMajor.sceneCollectionManagers[0].xLimitsx + 1), streamerMajor.sceneCollectionManagers[0].ySize * (streamerMajor.sceneCollectionManagers[0].yLimitsy - streamerMajor.sceneCollectionManagers[0].yLimitsx + 1), streamerMajor.sceneCollectionManagers[0].zSize * (streamerMajor.sceneCollectionManagers[0].zLimitsy - streamerMajor.sceneCollectionManagers[0].zLimitsx + 1));
		Vector3 vector = worldSize;
		Debug.Log("World Mover worldSize - " + vector.ToString());
	}

	public void Update()
	{
		if (streamerMajor.player != null)
		{
			playerPositionMovedLooped = streamerMajor.player.position - currentMove;
			if (streamerMajor.looping)
			{
				playerPositionMovedLooped = new Vector3((worldSize.x != 0f) ? (modf(playerPositionMovedLooped.x + (float)Mathf.Abs(streamerMajor.sceneCollectionManagers[0].xSize * streamerMajor.sceneCollectionManagers[0].xLimitsx), worldSize.x) + (float)(streamerMajor.sceneCollectionManagers[0].xSize * streamerMajor.sceneCollectionManagers[0].xLimitsx)) : playerPositionMovedLooped.x, (worldSize.y != 0f) ? (modf(playerPositionMovedLooped.y + (float)Mathf.Abs(streamerMajor.sceneCollectionManagers[0].ySize * streamerMajor.sceneCollectionManagers[0].yLimitsx), worldSize.y) + (float)(streamerMajor.sceneCollectionManagers[0].ySize * streamerMajor.sceneCollectionManagers[0].yLimitsx)) : playerPositionMovedLooped.y, (worldSize.z != 0f) ? (modf(playerPositionMovedLooped.z + (float)Mathf.Abs(streamerMajor.sceneCollectionManagers[0].zSize * streamerMajor.sceneCollectionManagers[0].zLimitsx), worldSize.z) + (float)(streamerMajor.sceneCollectionManagers[0].zSize * streamerMajor.sceneCollectionManagers[0].zLimitsx)) : playerPositionMovedLooped.z);
			}
		}
	}

	public void CheckMoverDistance(int xPosCurrent, int yPosCurrent, int zPosCurrent)
	{
		if (!waitForMover && (Mathf.Abs((float)xPosCurrent - xCurrentTile) > xTileRange || Mathf.Abs((float)yPosCurrent - yCurrentTile) > yTileRange || Mathf.Abs((float)zPosCurrent - zCurrentTile) > zTileRange))
		{
			waitForMover = true;
			StartCoroutine(MoveWorld(xPosCurrent, yPosCurrent, zPosCurrent));
		}
	}

	private IEnumerator MoveWorld(int xPosCurrent, int yPosCurrent, int zPosCurrent)
	{
		yield return new WaitForSeconds(waitForRestart);
		Vector3 vector = new Vector3(((float)xPosCurrent - xCurrentTile) * (float)streamerMajor.sceneCollectionManagers[0].xSize, ((float)yPosCurrent - yCurrentTile) * (float)streamerMajor.sceneCollectionManagers[0].ySize, ((float)zPosCurrent - zCurrentTile) * (float)streamerMajor.sceneCollectionManagers[0].zSize);
		currentMove -= vector;
		streamerMajor.player.position -= vector;
		foreach (SceneCollectionManager sceneCollectionManager in streamerMajor.sceneCollectionManagers)
		{
			foreach (SceneSplit loadedScene in sceneCollectionManager.loadedScenes)
			{
				if (loadedScene.loaded && loadedScene.sceneGo != null)
				{
					loadedScene.sceneGo.transform.position -= vector;
				}
			}
		}
		foreach (Transform item in objectsToMove)
		{
			if (item != null)
			{
				Vector3 position = item.position;
				position -= vector;
				item.position = position;
			}
		}
		xCurrentTile = xPosCurrent;
		yCurrentTile = yPosCurrent;
		zCurrentTile = zPosCurrent;
		streamerMajor.currentMove = currentMove;
		Streamer[] array = streamerMinors;
		foreach (Streamer obj in array)
		{
			obj.currentMove = currentMove;
			foreach (SceneCollectionManager sceneCollectionManager2 in obj.sceneCollectionManagers)
			{
				foreach (SceneSplit loadedScene2 in sceneCollectionManager2.loadedScenes)
				{
					if (loadedScene2.loaded && loadedScene2.sceneGo != null)
					{
						loadedScene2.sceneGo.transform.position -= vector;
					}
				}
			}
		}
		waitForMover = false;
	}

	public void MoveObject(Transform objectTransform)
	{
		objectTransform.position += currentMove;
	}

	public void AddObjectToMove(Transform objectToMove)
	{
		base.transform.position += currentMove;
		objectsToMove.Add(objectToMove);
	}

	private float modf(float x, float m)
	{
		return (x % m + m) % m;
	}
}
