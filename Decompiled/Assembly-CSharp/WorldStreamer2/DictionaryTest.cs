using System.Collections.Generic;
using UnityEngine;

namespace WorldStreamer2;

public class DictionaryTest : MonoBehaviour
{
	public Dictionary<int[], SceneSplit> scenesArray;

	public Dictionary<Vector3Int, SceneSplit> scenesArrayV;

	private void Start()
	{
		int num = 100;
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		scenesArray = new Dictionary<int[], SceneSplit>(new IntArrayComparerTest());
		for (int i = -num; i < num; i++)
		{
			for (int j = -num; j < num; j++)
			{
				for (int k = -num; k < num; k++)
				{
					int[] key = new int[3] { i, j, k };
					scenesArray.Add(key, new SceneSplit());
				}
			}
		}
		float num2 = Time.realtimeSinceStartup - realtimeSinceStartup;
		Debug.Log("Creation " + (Time.realtimeSinceStartup - realtimeSinceStartup));
		float realtimeSinceStartup2 = Time.realtimeSinceStartup;
		for (int l = -num; l < num; l++)
		{
			for (int m = -num; m < num; m++)
			{
				for (int n = -num; n < num; n++)
				{
					int[] key2 = new int[3] { l, m, n };
					if (scenesArray.ContainsKey(key2))
					{
						_ = scenesArray[key2];
					}
				}
			}
		}
		float num3 = Time.realtimeSinceStartup - realtimeSinceStartup2;
		Debug.Log("Loading " + (Time.realtimeSinceStartup - realtimeSinceStartup2));
		float realtimeSinceStartup3 = Time.realtimeSinceStartup;
		scenesArrayV = new Dictionary<Vector3Int, SceneSplit>(new Vector3ArrayComparerTest());
		for (int num4 = -num; num4 < num; num4++)
		{
			for (int num5 = -num; num5 < num; num5++)
			{
				for (int num6 = -num; num6 < num; num6++)
				{
					Vector3Int key3 = new Vector3Int(num4, num5, num6);
					scenesArrayV.Add(key3, new SceneSplit());
				}
			}
		}
		float num7 = Time.realtimeSinceStartup - realtimeSinceStartup3;
		Debug.Log("Creation " + (Time.realtimeSinceStartup - realtimeSinceStartup3));
		float realtimeSinceStartup4 = Time.realtimeSinceStartup;
		for (int num8 = -num; num8 < num; num8++)
		{
			for (int num9 = -num; num9 < num; num9++)
			{
				for (int num10 = -num; num10 < num; num10++)
				{
					Vector3Int key4 = new Vector3Int(num8, num9, num10);
					if (scenesArrayV.ContainsKey(key4))
					{
						_ = scenesArrayV[key4];
					}
				}
			}
		}
		float num11 = Time.realtimeSinceStartup - realtimeSinceStartup4;
		Debug.Log("Loading " + (Time.realtimeSinceStartup - realtimeSinceStartup4));
		Debug.Log("Loading Comp " + (num2 - num7));
		Debug.Log("Creation Comp " + (num3 - num11));
		float realtimeSinceStartup5 = Time.realtimeSinceStartup;
		int[] key5 = new int[3] { 10, 10, 10 };
		if (scenesArray.ContainsKey(key5))
		{
			_ = scenesArray[key5];
		}
		float num12 = Time.realtimeSinceStartup - realtimeSinceStartup5;
		Debug.Log("Single " + num12);
		float realtimeSinceStartup6 = Time.realtimeSinceStartup;
		Vector3Int key6 = new Vector3Int(10, 10, 10);
		if (scenesArrayV.ContainsKey(key6))
		{
			_ = scenesArrayV[key6];
		}
		float num13 = Time.realtimeSinceStartup - realtimeSinceStartup6;
		Debug.Log("Single V " + num13);
		Debug.Log("Single Comp " + (num12 - num13));
	}
}
