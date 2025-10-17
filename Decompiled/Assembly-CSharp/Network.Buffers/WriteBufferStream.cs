using System;
using System.IO;

namespace Network.Buffers;

public class WriteBufferStream : Stream
{
	private byte[] _buffer;

	private int _position;

	public override bool CanRead => false;

	public override bool CanSeek => false;

	public override bool CanWrite => true;

	public override long Length => _position;

	public override long Position
	{
		get
		{
			return _position;
		}
		set
		{
			_position = (int)value;
		}
	}

	public ArraySegment<byte> ArraySegment => new ArraySegment<byte>(_buffer, 0, _position);

	public WriteBufferStream(byte[] buffer)
	{
		_buffer = buffer;
		_position = 0;
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		if (_position + count > _buffer.Length)
		{
			Array.Resize(ref _buffer, (_buffer.Length + count) * 2);
		}
		Array.Copy(buffer, offset, _buffer, _position, count);
		_position += count;
	}

	public override void Flush()
	{
		_position = 0;
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		throw new NotSupportedException();
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		throw new NotSupportedException();
	}

	public override void SetLength(long value)
	{
		_position = 0;
	}
}
