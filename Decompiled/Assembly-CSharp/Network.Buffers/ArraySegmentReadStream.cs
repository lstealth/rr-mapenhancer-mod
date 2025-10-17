using System;
using System.IO;

namespace Network.Buffers;

public class ArraySegmentReadStream : Stream
{
	private ArraySegment<byte> _arraySegment;

	public override bool CanRead => true;

	public override bool CanSeek => false;

	public override bool CanWrite => false;

	public override long Length => _arraySegment.Count;

	public override long Position { get; set; }

	public void SetArraySegment(ArraySegment<byte> arraySegment)
	{
		_arraySegment = arraySegment;
		Position = 0L;
	}

	public override void Flush()
	{
		Position = 0L;
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		if (_arraySegment.Array == null)
		{
			return 0;
		}
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if (offset < 0)
		{
			throw new ArgumentOutOfRangeException("offset", "Offset must be non-negative.");
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", "Count must be non-negative.");
		}
		if (buffer.Length - offset < count)
		{
			throw new ArgumentException("The buffer length minus offset is less than count.");
		}
		int num = _arraySegment.Count - (int)Position;
		if (num <= 0)
		{
			return 0;
		}
		int num2 = Math.Min(num, count);
		Array.Copy(_arraySegment.Array, _arraySegment.Offset + (int)Position, buffer, offset, num2);
		Position += num2;
		return num2;
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		throw new NotImplementedException();
	}

	public override void SetLength(long value)
	{
		throw new NotImplementedException();
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		throw new NotImplementedException();
	}
}
