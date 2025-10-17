using System;
using System.Collections;

namespace Track.Signals;

internal static class CoroutineUtils
{
	public static IEnumerator RunThrowingIterator(IEnumerator enumerator, Action<Exception> done)
	{
		while (true)
		{
			object current;
			try
			{
				if (!enumerator.MoveNext())
				{
					break;
				}
				current = enumerator.Current;
				goto IL_0046;
			}
			catch (Exception obj)
			{
				done(obj);
				yield break;
			}
			IL_0046:
			yield return current;
		}
		done(null);
	}
}
