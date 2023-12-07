using System.Text;

namespace echo.primary.core.tcp;

public class BytesBuffer {
	private byte[] _data;
	private long _size;
	private long _offset;
	private TaskCompletionSource? _ps = null;

	public bool IsEmpty => _size == 0;

	public byte[] Data => _data;

	public long Capacity => _data.Length;

	public long Size => _size;

	public long Offset => _offset;

	public BytesBuffer() {
		_data = Array.Empty<byte>();
		_size = 0;
		_offset = 0;
	}

	public BytesBuffer(long cap) {
		_data = new byte[cap];
		_size = 0;
		_offset = 0;
	}

	public Span<byte> AsSpan() {
		return new Span<byte>(_data, (int)_offset, (int)_size);
	}

	public BytesBuffer Clear() {
		_size = 0;
		_offset = 0;
		return this;
	}

	public BytesBuffer Reserve(long ncap) {
		if (ncap <= Capacity) return this;

		var ndata = new byte[Math.Max(ncap, Capacity << 1)];
		Array.Copy(_data, 0, ndata, 0, _size);
		_data = ndata;
		return this;
	}

	public void Write(byte v) {
		Reserve(_size + 1);
		_data[_size] = v;
		_size++;
		_ps?.SetResult();
	}

	public void Write(byte[] buf) {
		Reserve(_size + buf.Length);
		Array.Copy(buf, 0, _data, _size, buf.Length);
		_size += buf.Length;
		_ps?.SetResult();
	}

	public void Write(byte[] buf, long offset, long size) {
		Reserve(_size + size);
		Array.Copy(buf, offset, _data, _size, size);
		_size += size;
		_ps?.SetResult();
	}

	public void Write(byte[] buf, long offset) {
		var size = buf.Length - offset;
		Reserve(_size + size);
		Array.Copy(buf, offset, _data, _size, size);
		_size += size;
		_ps?.SetResult();
	}

	public void Write(ReadOnlySpan<byte> buffer) {
		Reserve(_size + buffer.Length);
		buffer.CopyTo(new Span<byte>(_data, (int)_size, buffer.Length));
		_size += buffer.Length;
		_ps?.SetResult();
	}

	public void Write(BytesBuffer buffer) => Write(buffer.AsSpan());

	public void Write(string text) {
		Reserve(_size + Encoding.UTF8.GetMaxByteCount(text.Length));
		_size += Encoding.UTF8.GetBytes(text, 0, text.Length, _data, (int)_size);
		_ps?.SetResult();
	}

	public async Task ReadN(byte[] buf, long required) {
		var remain = _size - _offset;
	}
}