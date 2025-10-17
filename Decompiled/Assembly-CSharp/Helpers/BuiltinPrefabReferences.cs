using System;
using System.Collections.Generic;
using Model;
using UnityEngine;

namespace Helpers;

[CreateAssetMenu(fileName = "Prefab References", menuName = "Railroader/Builtin Prefab References", order = 0)]
public class BuiltinPrefabReferences : ScriptableObject, IPrefabInstantiator
{
	[Serializable]
	private struct Entry
	{
		public string name;

		public GameObject gameObject;
	}

	[SerializeField]
	private List<Entry> entries;

	public T InstantiatePrefab<T>(string entryName, Transform parent) where T : Component
	{
		foreach (Entry entry in entries)
		{
			if (!(entry.name != entryName))
			{
				T component = entry.gameObject.GetComponent<T>();
				if (component != null)
				{
					return UnityEngine.Object.Instantiate(component.gameObject, parent, worldPositionStays: false).GetComponent<T>();
				}
			}
		}
		throw new ArgumentException($"Couldn't find component {typeof(T)} with name {entryName}");
	}
}
