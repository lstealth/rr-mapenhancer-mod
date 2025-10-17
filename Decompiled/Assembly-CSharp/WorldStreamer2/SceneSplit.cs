using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WorldStreamer2;

[Serializable]
public class SceneSplit
{
	public int posX;

	public int posY;

	public int posZ;

	public string sceneName;

	public Scene scene;

	public GameObject sceneGo;

	public bool loaded;

	public bool loadingFinished;

	public float posXLimitMove;

	public int xDeloadLimit;

	public float posYLimitMove;

	public int yDeloadLimit;

	public float posZLimitMove;

	public int zDeloadLimit;

	public int basePosX;

	public int basePosY;

	public int basePosZ;

	public SceneCollectionManager sceneCollectionManager;
}
