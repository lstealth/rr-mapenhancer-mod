using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorldStreamer2;

[Serializable]
public class SceneCollectionManager : ScriptableObject
{
	public bool active = true;

	public int priority = 1;

	[Tooltip("Amount of max grid elements that you want to start loading in one frame.")]
	public int maxParallelSceneLoading = 1;

	[Tooltip("Distance in grid elements that you want hold loaded.")]
	public Vector3Int loadingRange = new Vector3Int(3, 3, 3);

	[Tooltip("Enables ring streaming.")]
	public bool useLoadingRangeMin;

	[Tooltip("Area that you want to cutout from loading range.")]
	public Vector3Int loadingRangeMin = new Vector3Int(2, 2, 2);

	[Tooltip("Enables ccene collection elements to unload when scene loaded.")]
	public bool useUnloadRangeConnect;

	[Tooltip("Scene collection elements to unload when scene loaded.")]
	public SceneCollectionManager unloadRangeConnect;

	[HideInInspector]
	[Tooltip("Scene collection elements to unload when scene loaded.")]
	public SceneCollectionManager unloadRangeConnectParent;

	[Tooltip("Distance in grid elements after which you want to unload assets.")]
	public Vector3Int deloadingRange = new Vector3Int(3, 3, 3);

	[Header("Settings")]
	public string prefixName = "stream";

	public string prefixScene = "Scene";

	public string path = "Assets/WorldStreamer/SplitScenes/";

	[Space(5f)]
	public string[] names;

	[Space(5f)]
	public bool xSplitIs = true;

	public bool ySplitIs;

	public bool zSplitIs = true;

	[Space(5f)]
	public int xSize = 500;

	public int ySize = 500;

	public int zSize = 500;

	[Space(5f)]
	public int xLimitsx = int.MaxValue;

	public int xLimitsy = int.MinValue;

	public int yLimitsx = int.MaxValue;

	public int yLimitsy = int.MinValue;

	public int zLimitsx = int.MaxValue;

	public int zLimitsy = int.MinValue;

	[HideInInspector]
	public int xLimitsScenex = int.MaxValue;

	[HideInInspector]
	public int xLimitsSceney = int.MinValue;

	[HideInInspector]
	public int yLimitsScenex = int.MaxValue;

	[HideInInspector]
	public int yLimitsSceney = int.MinValue;

	[HideInInspector]
	public int zLimitsScenex = int.MaxValue;

	[HideInInspector]
	public int zLimitsSceney = int.MinValue;

	[Space(5f)]
	[HideInInspector]
	public bool collapsed = true;

	[HideInInspector]
	public int layerNumber;

	[Space(5f)]
	public Color color = Color.red;

	public bool showDebug;

	public Dictionary<Vector3Int, SceneSplit> scenesArray;

	[HideInInspector]
	public int xLoadingLimity;

	[HideInInspector]
	public int xLoadingLimitx;

	[HideInInspector]
	public int xLoadingRange;

	[HideInInspector]
	public int yLoadingLimity;

	[HideInInspector]
	public int yLoadingLimitx;

	[HideInInspector]
	public int yLoadingRange;

	[HideInInspector]
	public int zLoadingLimity;

	[HideInInspector]
	public int zLoadingLimitx;

	[HideInInspector]
	public int zLoadingRange;

	[HideInInspector]
	public int xPos;

	[HideInInspector]
	public int yPos;

	[HideInInspector]
	public int zPos;

	[HideInInspector]
	public int currentlySceneLoading;

	[HideInInspector]
	public List<SceneSplit> loadedScenes = new List<SceneSplit>();

	public void GetSceneCollectionWolrdLimits(out Vector2Int xLimits, out Vector2Int yLimits, out Vector2Int zLimits)
	{
		xLimits = new Vector2Int(xLimitsx * xSize, (xLimitsy + 1) * xSize);
		yLimits = new Vector2Int(yLimitsx * ySize, (yLimitsy + 1) * ySize);
		zLimits = new Vector2Int(zLimitsx * zSize, (zLimitsy + 1) * zSize);
	}

	public void ResetPosition()
	{
		xPos = int.MinValue;
		yPos = int.MinValue;
		zPos = int.MinValue;
	}

	public bool CheckPosition(Vector3 pos)
	{
		int num = ((xSize != 0) ? Mathf.FloorToInt(pos.x / (float)xSize) : 0);
		int num2 = ((ySize != 0) ? Mathf.FloorToInt(pos.y / (float)ySize) : 0);
		int num3 = ((zSize != 0) ? Mathf.FloorToInt(pos.z / (float)zSize) : 0);
		if (num != xPos || num2 != yPos || num3 != zPos)
		{
			xPos = num;
			yPos = num2;
			zPos = num3;
			return true;
		}
		return false;
	}

	public Vector3Int GetTilePosition(Vector3 pos)
	{
		int x = ((xSize != 0) ? Mathf.FloorToInt(pos.x / (float)xSize) : 0);
		int y = ((ySize != 0) ? Mathf.FloorToInt(pos.y / (float)ySize) : 0);
		int z = ((zSize != 0) ? Mathf.FloorToInt(pos.z / (float)zSize) : 0);
		return new Vector3Int(x, y, z);
	}

	public void CalculateLoadingLimits(bool looping, bool overideRangeLimit)
	{
		CalculateLoadingLimits(looping, overideRangeLimit, overideScenesLimits: false, Vector2Int.zero, Vector2Int.zero, Vector2Int.zero);
	}

	public void CalculateLoadingLimits(bool looping, bool overideRangeLimit, bool overideScenesLimits, Vector2Int xLimits, Vector2Int yLimits, Vector2Int zLimits)
	{
		loadedScenes.Clear();
		currentlySceneLoading = 0;
		xLoadingLimity = xLimitsy;
		xLoadingLimitx = xLimitsx;
		xLoadingRange = xLoadingLimity - xLoadingLimitx + 1;
		yLoadingLimity = yLimitsy;
		yLoadingLimitx = yLimitsx;
		yLoadingRange = yLoadingLimity - yLoadingLimitx + 1;
		zLoadingLimity = zLimitsy;
		zLoadingLimitx = zLimitsx;
		zLoadingRange = zLoadingLimity - zLoadingLimitx + 1;
		if (overideScenesLimits)
		{
			if (xSize != 0)
			{
				xLoadingLimity = xLimits.y / xSize - 1;
				xLoadingLimitx = xLimits.x / xSize;
				xLoadingRange = xLoadingLimity - xLoadingLimitx + 1;
			}
			if (ySize != 0)
			{
				yLoadingLimity = yLimits.y / ySize - 1;
				yLoadingLimitx = yLimits.x / ySize;
				yLoadingRange = yLoadingLimity - yLoadingLimitx + 1;
			}
			if (zSize != 0)
			{
				zLoadingLimity = zLimits.y / zSize - 1;
				zLoadingLimitx = zLimits.x / zSize;
				zLoadingRange = zLoadingLimity - zLoadingLimitx + 1;
			}
		}
		if (looping && overideRangeLimit)
		{
			if ((xLoadingLimitx != 0 || xLoadingLimity != 0) && deloadingRange.x != 0)
			{
				int num = Mathf.CeilToInt((Mathf.Ceil((float)(deloadingRange.x * 2) / (float)xLoadingRange) - 0.5f) * 0.5f);
				xLoadingLimitx -= num * xLoadingRange;
				xLoadingLimity += num * xLoadingRange;
				xLoadingRange = xLoadingLimity - xLoadingLimitx + 1;
			}
			if ((yLoadingLimitx != 0 || yLoadingLimity != 0) && deloadingRange.y != 0)
			{
				int num2 = Mathf.CeilToInt((Mathf.Ceil((float)(deloadingRange.y * 2) / (float)yLoadingRange) - 0.5f) * 0.5f);
				yLoadingLimitx -= num2 * yLoadingRange;
				yLoadingLimity += num2 * yLoadingRange;
				yLoadingRange = yLoadingLimity - yLoadingLimitx + 1;
			}
			if ((zLoadingLimitx != 0 || zLoadingLimity != 0) && deloadingRange.z != 0)
			{
				int num3 = Mathf.CeilToInt((Mathf.Ceil((float)(deloadingRange.z * 2) / (float)zLoadingRange) - 0.5f) * 0.5f);
				zLoadingLimitx -= num3 * zLoadingRange;
				zLoadingLimity += num3 * zLoadingRange;
				zLoadingRange = zLoadingLimity - zLoadingLimitx + 1;
			}
		}
		xLimitsScenex = xLimitsx;
		xLimitsSceney = xLimitsy;
		yLimitsScenex = yLimitsx;
		yLimitsSceney = yLimitsy;
		zLimitsScenex = zLimitsx;
		zLimitsSceney = zLimitsy;
	}

	public void PrepareScenesArray(bool overideRangeLimit)
	{
		PrepareScenesArray(overideRangeLimit, overideScenesLimits: false, Vector2Int.zero, Vector2Int.zero, Vector2Int.zero);
	}

	public void PrepareScenesArray(bool overideRangeLimit, bool overideScenesLimits, Vector2Int xLimits, Vector2Int yLimits, Vector2Int zLimits)
	{
		scenesArray = new Dictionary<Vector3Int, SceneSplit>(new Vector3IntArrayComparer());
		string[] array = names;
		foreach (string text in array)
		{
			int posX = 0;
			int posY = 0;
			int posZ = 0;
			Streamer.SceneNameToPos(this, text, out posX, out posY, out posZ);
			SceneSplit sceneSplit = new SceneSplit();
			sceneSplit.posX = posX;
			sceneSplit.posY = posY;
			sceneSplit.posZ = posZ;
			sceneSplit.basePosX = posX;
			sceneSplit.basePosY = posY;
			sceneSplit.basePosZ = posZ;
			sceneSplit.sceneName = text.Replace(".unity", "");
			sceneSplit.sceneCollectionManager = this;
			sceneSplit.loadingFinished = false;
			scenesArray.Add(new Vector3Int(posX, posY, posZ), sceneSplit);
		}
		int num = xLimitsy;
		int num2 = xLimitsx;
		int m = num - num2 + 1;
		int num3 = yLimitsy;
		int num4 = yLimitsx;
		int m2 = num3 - num4 + 1;
		int num5 = zLimitsy;
		int num6 = zLimitsx;
		int m3 = num5 - num6 + 1;
		if (overideScenesLimits)
		{
			if (xSize != 0)
			{
				int num7 = xLimits.y / xSize - 1;
				num2 = xLimits.x / xSize;
				m = num7 - num2 + 1;
			}
			if (ySize != 0)
			{
				int num8 = yLimits.y / ySize - 1;
				num4 = yLimits.x / ySize;
				m2 = num8 - num4 + 1;
			}
			if (zSize != 0)
			{
				int num9 = zLimits.y / zSize - 1;
				num6 = zLimits.x / zSize;
				m3 = num9 - num6 + 1;
			}
		}
		if (!overideRangeLimit)
		{
			return;
		}
		for (int j = xLoadingLimitx; j <= xLoadingLimity; j++)
		{
			for (int k = yLoadingLimitx; k <= yLoadingLimity; k++)
			{
				for (int l = zLoadingLimitx; l <= zLoadingLimity; l++)
				{
					Vector3Int key = new Vector3Int(j, k, l);
					int x = Streamer.mod(j + Mathf.Abs(num2), m) + num2;
					int y = Streamer.mod(k + Mathf.Abs(num4), m2) + num4;
					int z = Streamer.mod(l + Mathf.Abs(num6), m3) + num6;
					if (!scenesArray.ContainsKey(key))
					{
						key = new Vector3Int(x, y, z);
						if (scenesArray.ContainsKey(key))
						{
							SceneSplit sceneSplit2 = scenesArray[key];
							SceneSplit sceneSplit3 = new SceneSplit();
							sceneSplit3.posX = j;
							sceneSplit3.posY = k;
							sceneSplit3.posZ = l;
							sceneSplit3.basePosX = sceneSplit2.basePosX;
							sceneSplit3.basePosY = sceneSplit2.basePosY;
							sceneSplit3.basePosZ = sceneSplit2.basePosZ;
							sceneSplit3.sceneName = sceneSplit2.sceneName;
							sceneSplit3.sceneCollectionManager = this;
							sceneSplit3.loadingFinished = false;
							scenesArray.Add(new Vector3Int(j, k, l), sceneSplit3);
						}
					}
				}
			}
		}
	}

	public static Vector3Int SceneNameToVectorIntPos(SceneCollectionManager sceneCollectionManager, string sceneName)
	{
		int x = 0;
		int y = 0;
		int z = 0;
		string[] array = sceneName.Replace(sceneCollectionManager.prefixScene, "").Replace(".unity", "").Split(new char[1] { '_' }, StringSplitOptions.RemoveEmptyEntries);
		foreach (string text in array)
		{
			if (text[0] == 'x')
			{
				x = int.Parse(text.Replace("x", ""));
			}
			if (text[0] == 'y')
			{
				y = int.Parse(text.Replace("y", ""));
			}
			if (text[0] == 'z')
			{
				z = int.Parse(text.Replace("z", ""));
			}
		}
		return new Vector3Int(x, y, z);
	}
}
