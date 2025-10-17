using System.Collections.Generic;

namespace Helpers;

public static class HashSetExtensions
{
	public static T SomeElement<T>(this HashSet<T> set)
	{
		T result = default(T);
		using HashSet<T>.Enumerator enumerator = set.GetEnumerator();
		if (enumerator.MoveNext())
		{
			return enumerator.Current;
		}
		return result;
	}
}
