using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Helpers;

public static class TransformExtensions
{
	public static List<GameObject> FindObjectsWithTag(this Transform parent, string tag)
	{
		List<GameObject> list = new List<GameObject>();
		foreach (Transform item in parent)
		{
			if (item.CompareTag(tag))
			{
				list.Add(item.gameObject);
			}
			if (item.childCount > 0)
			{
				list.AddRange(item.FindObjectsWithTag(tag));
			}
		}
		return list;
	}

	public static Vector3 GamePosition(this Transform t)
	{
		return WorldTransformer.WorldToGame(t.position);
	}

	public static void DestroyAllChildren(this Transform transform)
	{
		for (int num = transform.childCount - 1; num >= 0; num--)
		{
			Transform child = transform.GetChild(num);
			if (Application.isPlaying)
			{
				Object.Destroy(child.gameObject);
			}
			else
			{
				Object.DestroyImmediate(child.gameObject);
			}
		}
	}

	public static string HierarchyString(this Transform t)
	{
		StringBuilder sb = new StringBuilder();
		if (t.parent == null)
		{
			return t.name;
		}
		Spelunk(t.parent);
		sb.Append(t.name);
		return sb.ToString();
		void Spelunk(Transform transform)
		{
			if (transform.parent == null)
			{
				sb.Append(transform.name + "->");
			}
			else
			{
				Spelunk(transform.parent);
				sb.Append(transform.name + "->");
			}
		}
	}
}
