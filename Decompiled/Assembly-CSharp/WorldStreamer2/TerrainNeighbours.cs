using System.Collections.Generic;
using UnityEngine;

namespace WorldStreamer2;

public class TerrainNeighbours : MonoBehaviour
{
	public List<Terrain> terrainsToOmit;

	[Tooltip("If you use Floating Point fix system drag and drop world mover prefab from your scene hierarchy.")]
	public WorldMover worldMover;

	public List<Terrain> _terrains = new List<Terrain>();

	private Dictionary<int[], Terrain> _terrainDict;

	[Tooltip("Debug value, it gives info about starting position offset.")]
	private Vector2 firstPosition;

	private int sizeX;

	private int sizeZ;

	private bool firstPositonSet;

	private void Start()
	{
		CreateNeighbours();
	}

	public void CreateNeighbours()
	{
		List<Terrain> list = new List<Terrain>();
		list.AddRange(Terrain.activeTerrains);
		foreach (Terrain item in terrainsToOmit)
		{
			if (list.Contains(item))
			{
				list.Remove(item);
			}
		}
		foreach (Terrain terrain in _terrains)
		{
			if (list.Contains(terrain))
			{
				list.Remove(terrain);
			}
		}
		if (_terrainDict == null)
		{
			_terrainDict = new Dictionary<int[], Terrain>(new IntArrayComparer());
		}
		Dictionary<int[], Terrain> dictionary = new Dictionary<int[], Terrain>(new IntArrayComparer());
		Dictionary<int[], Terrain> dictionary2 = new Dictionary<int[], Terrain>(new IntArrayComparer());
		if (list.Count <= 0)
		{
			return;
		}
		if (!firstPositonSet)
		{
			firstPositonSet = true;
			firstPosition = new Vector2(list[0].transform.position.x, list[0].transform.position.z);
			sizeX = (int)list[0].terrainData.size.x;
			sizeZ = (int)list[0].terrainData.size.z;
		}
		foreach (Terrain item2 in list)
		{
			_terrains.Add(item2);
			Vector3 position = item2.transform.position;
			if (worldMover != null)
			{
				position -= worldMover.currentMove;
			}
			int[] key = new int[2]
			{
				Mathf.RoundToInt((position.x - firstPosition.x) / (float)sizeX),
				Mathf.RoundToInt((position.z - firstPosition.y) / (float)sizeZ)
			};
			if (_terrainDict.ContainsKey(key))
			{
				_terrainDict[key] = item2;
			}
			else
			{
				_terrainDict.Add(key, item2);
			}
			dictionary.Add(key, item2);
		}
		foreach (KeyValuePair<int[], Terrain> item3 in dictionary)
		{
			int[] key2 = item3.Key;
			Terrain value = null;
			Terrain value2 = null;
			Terrain value3 = null;
			Terrain value4 = null;
			int[] key3 = new int[2]
			{
				key2[0],
				key2[1] + 1
			};
			_terrainDict.TryGetValue(key3, out value);
			int[] key4 = new int[2]
			{
				key2[0] - 1,
				key2[1]
			};
			_terrainDict.TryGetValue(key4, out value2);
			int[] key5 = new int[2]
			{
				key2[0] + 1,
				key2[1]
			};
			_terrainDict.TryGetValue(key5, out value3);
			int[] key6 = new int[2]
			{
				key2[0],
				key2[1] - 1
			};
			_terrainDict.TryGetValue(key6, out value4);
			item3.Value.SetNeighbors(value2, value, value3, value4);
			item3.Value.Flush();
			if (value != null && !dictionary2.ContainsKey(key3))
			{
				dictionary2.Add(key3, value);
			}
			if (value2 != null && !dictionary2.ContainsKey(key4))
			{
				dictionary2.Add(key4, value2);
			}
			if (value3 != null && !dictionary2.ContainsKey(key5))
			{
				dictionary2.Add(key5, value3);
			}
			if (value4 != null && !dictionary2.ContainsKey(key6))
			{
				dictionary2.Add(key6, value4);
			}
		}
		foreach (KeyValuePair<int[], Terrain> item4 in dictionary2)
		{
			int[] key7 = item4.Key;
			Terrain value5 = null;
			Terrain value6 = null;
			Terrain value7 = null;
			Terrain value8 = null;
			int[] key8 = new int[2]
			{
				key7[0],
				key7[1] + 1
			};
			_terrainDict.TryGetValue(key8, out value5);
			int[] key9 = new int[2]
			{
				key7[0] - 1,
				key7[1]
			};
			_terrainDict.TryGetValue(key9, out value6);
			int[] key10 = new int[2]
			{
				key7[0] + 1,
				key7[1]
			};
			_terrainDict.TryGetValue(key10, out value7);
			int[] key11 = new int[2]
			{
				key7[0],
				key7[1] - 1
			};
			_terrainDict.TryGetValue(key11, out value8);
			item4.Value.SetNeighbors(value6, value5, value7, value8);
			item4.Value.Flush();
		}
	}
}
