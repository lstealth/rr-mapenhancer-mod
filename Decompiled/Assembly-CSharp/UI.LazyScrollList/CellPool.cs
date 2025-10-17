using System;
using System.Collections.Generic;

namespace UI.LazyScrollList;

public class CellPool<T> where T : ILazyScrollListCell
{
	private readonly Stack<T> _pool = new Stack<T>();

	private readonly Func<T> _createFunc;

	public CellPool(Func<T> createFunc, int initialSize = 0)
	{
		_createFunc = createFunc;
		for (int i = 0; i < initialSize; i++)
		{
			T item = createFunc();
			item.RectTransform.gameObject.SetActive(value: false);
			_pool.Push(item);
		}
	}

	public T GetObject()
	{
		if (_pool.Count > 0)
		{
			T result = _pool.Pop();
			result.RectTransform.gameObject.SetActive(value: true);
			return result;
		}
		return _createFunc();
	}

	public void ReturnObject(T obj)
	{
		obj.RectTransform.gameObject.SetActive(value: false);
		_pool.Push(obj);
	}
}
