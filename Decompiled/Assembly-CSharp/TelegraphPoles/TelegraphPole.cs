using System;
using UnityEngine;

namespace TelegraphPoles;

public class TelegraphPole : MonoBehaviour
{
	[Serializable]
	public struct Row
	{
		public Vector3[] points;
	}

	public Row[] rows;

	public Vector3 localBasePosition;

	public int CountPoints()
	{
		int num = 0;
		Row[] array = rows;
		for (int i = 0; i < array.Length; i++)
		{
			Row row = array[i];
			num += row.points.Length;
		}
		return num;
	}
}
