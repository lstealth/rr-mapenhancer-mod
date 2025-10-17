using System.Collections.Generic;
using UnityEngine;

namespace TerrainStitch;

public class TerrainNeighbours : MonoBehaviour
{
	private Terrain[] _terrains;

	private Dictionary<int[], Terrain> _terrainDict;

	public Vector2 firstPosition;

	private void Start()
	{
		CreateNeighbours();
	}

	public void CreateNeighbours()
	{
		if (_terrainDict == null)
		{
			_terrainDict = new Dictionary<int[], Terrain>(new IntArrayComparer());
		}
		else
		{
			_terrainDict.Clear();
		}
		_terrains = Terrain.activeTerrains;
		if (_terrains.Length == 0)
		{
			return;
		}
		firstPosition = new Vector2(_terrains[0].transform.position.x, _terrains[0].transform.position.z);
		int num = (int)_terrains[0].terrainData.size.x;
		int num2 = (int)_terrains[0].terrainData.size.z;
		Terrain[] terrains = _terrains;
		foreach (Terrain terrain in terrains)
		{
			int[] key = new int[2]
			{
				Mathf.RoundToInt((terrain.transform.position.x - firstPosition.x) / (float)num),
				Mathf.RoundToInt((terrain.transform.position.z - firstPosition.y) / (float)num2)
			};
			_terrainDict.Add(key, terrain);
		}
		foreach (KeyValuePair<int[], Terrain> item in _terrainDict)
		{
			int[] key2 = item.Key;
			Terrain value = null;
			Terrain value2 = null;
			Terrain value3 = null;
			Terrain value4 = null;
			_terrainDict.TryGetValue(new int[2]
			{
				key2[0],
				key2[1] + 1
			}, out value);
			_terrainDict.TryGetValue(new int[2]
			{
				key2[0] - 1,
				key2[1]
			}, out value2);
			_terrainDict.TryGetValue(new int[2]
			{
				key2[0] + 1,
				key2[1]
			}, out value3);
			_terrainDict.TryGetValue(new int[2]
			{
				key2[0],
				key2[1] - 1
			}, out value4);
			item.Value.SetNeighbors(value2, value, value3, value4);
			item.Value.Flush();
		}
	}
}
