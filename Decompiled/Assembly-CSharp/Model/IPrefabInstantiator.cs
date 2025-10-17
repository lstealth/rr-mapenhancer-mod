using UnityEngine;

namespace Model;

public interface IPrefabInstantiator
{
	T InstantiatePrefab<T>(string name, Transform parent) where T : Component;
}
