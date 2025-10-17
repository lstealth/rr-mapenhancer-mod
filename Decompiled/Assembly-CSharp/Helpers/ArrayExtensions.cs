using UnityEngine;

namespace Helpers;

public static class ArrayExtensions
{
	public static T RandomElement<T>(this T[] array)
	{
		return array[Random.Range(0, array.Length)];
	}
}
