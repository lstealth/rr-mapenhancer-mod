using System;
using System.Buffers;

namespace Network.Buffers;

public class NetworkBufferWriter : IBufferWriter<byte>, IDisposable
{
	private const int MinimumBufferSize = 1024;

	private byte[] _rentedBuffer;

	private int _written;

	public byte[] Array => _rentedBuffer;

	public int ArrayLength => _written;

	public NetworkBufferWriter()
	{
		_rentedBuffer = ArrayPool<byte>.Shared.Rent(1024);
	}

	public void Dispose()
	{
		if (_rentedBuffer != null)
		{
			ArrayPool<byte>.Shared.Return(_rentedBuffer, clearArray: true);
			_rentedBuffer = null;
			_written = 0;
		}
	}

	public void Clear()
	{
		AssertNotDisposed();
		_rentedBuffer.AsSpan(0, _written).Clear();
		_written = 0;
	}

	public void Reset()
	{
		Clear();
		if (_rentedBuffer.Length > 1024)
		{
			ArrayPool<byte>.Shared.Return(_rentedBuffer, clearArray: true);
			_rentedBuffer = ArrayPool<byte>.Shared.Rent(1024);
		}
	}

	public void Advance(int count)
	{
		if (_written > _rentedBuffer.Length - count)
		{
			throw new InvalidOperationException("Cannot advance past the end of the buffer.");
		}
		_written += count;
	}

	public Memory<byte> GetMemory(int sizeHint = 0)
	{
		AssertNotDisposed();
		CheckAndResizeBuffer(sizeHint);
		return _rentedBuffer.AsMemory(_written);
	}

	public Span<byte> GetSpan(int sizeHint = 0)
	{
		AssertNotDisposed();
		CheckAndResizeBuffer(sizeHint);
		return _rentedBuffer.AsSpan(_written);
	}

	private void CheckAndResizeBuffer(int sizeHint)
	{
		if (sizeHint == 0)
		{
			sizeHint = 1024;
		}
		int num = _rentedBuffer.Length - _written;
		if (sizeHint > num)
		{
			int num2 = ((sizeHint > _rentedBuffer.Length) ? sizeHint : _rentedBuffer.Length);
			int minimumLength = checked(_rentedBuffer.Length + num2);
			byte[] rentedBuffer = _rentedBuffer;
			_rentedBuffer = ArrayPool<byte>.Shared.Rent(minimumLength);
			rentedBuffer.AsSpan(0, _written).CopyTo(_rentedBuffer);
			ArrayPool<byte>.Shared.Return(rentedBuffer, clearArray: true);
		}
	}

	private void AssertNotDisposed()
	{
		if (_rentedBuffer == null)
		{
			throw new ObjectDisposedException("NetworkBufferWriter");
		}
	}
}
