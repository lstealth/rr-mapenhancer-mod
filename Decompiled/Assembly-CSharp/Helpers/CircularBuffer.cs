using System;
using System.Collections;
using System.Collections.Generic;

namespace Helpers;

public class CircularBuffer<T> : IEnumerable<T>, IEnumerable
{
	private readonly T[] _buffer;

	private int _head;

	private int _tail;

	private int _length;

	private readonly int _bufferSize;

	private readonly object _lock = new object();

	public bool IsEmpty => _length == 0;

	public bool IsFull => _length == _bufferSize;

	public int Length => _length;

	public int Capacity => _bufferSize;

	public CircularBuffer(int bufferSize)
	{
		_buffer = new T[bufferSize];
		_bufferSize = bufferSize;
		_head = bufferSize - 1;
	}

	public T Peek(int offset = 0)
	{
		lock (_lock)
		{
			if (_length <= offset)
			{
				throw new InvalidOperationException("Invalid offset");
			}
			return _buffer[(_tail + offset) % _bufferSize];
		}
	}

	public T Dequeue()
	{
		lock (_lock)
		{
			if (IsEmpty)
			{
				throw new InvalidOperationException("Queue exhausted");
			}
			T result = _buffer[_tail];
			_tail = NextPosition(_tail);
			_length--;
			return result;
		}
	}

	public void Enqueue(T toAdd)
	{
		lock (_lock)
		{
			_head = NextPosition(_head);
			_buffer[_head] = toAdd;
			if (IsFull)
			{
				_tail = NextPosition(_tail);
			}
			else
			{
				_length++;
			}
		}
	}

	private int NextPosition(int position)
	{
		return (position + 1) % _bufferSize;
	}

	public void Clear()
	{
		_length = 0;
		_head = _bufferSize - 1;
		_tail = 0;
	}

	public IEnumerator<T> GetEnumerator()
	{
		for (int i = 0; i < _length; i++)
		{
			yield return _buffer[(_tail + i) % _bufferSize];
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
