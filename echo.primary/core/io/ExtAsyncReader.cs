using System.Text;
using echo.primary.utils;

namespace echo.primary.core.io;

public class ExtAsyncReader(
	IAsyncReader src,
	int tmpCap = 4096,
	int tmpReadSize = 512,
	int srcReadSize = 4096
) : IAsyncReader {
	private readonly BytesBuffer tmp = new(tmpCap);
	private readonly byte[] tmpReadBuf = new byte[tmpReadSize];
	private readonly byte[] srcReadBuf = new byte[srcReadSize];

	private async Task EnsureTmpCanRead(int timeoutMills) {
		if (tmp.Stream.Position < tmp.Stream.Length) return;

		var rl = await src.Read(srcReadBuf, timeoutMills);
		if (rl < 1) {
			throw new Exception("empty read from src");
		}

		tmp.Stream.Position = 0;
		tmp.Writer.Write(srcReadBuf.AsSpan()[..rl]);
		tmp.Stream.Position = 0;
	}

	public async Task ReadUntil(MemoryStream ms, byte target, int timeoutMills = 0, int maxBytesSize = 0) {
		ms.Position = 0;

		var begin = Time.unixmills();
		while (true) {
			var remainMills = -1;
			if (timeoutMills > 0) {
				remainMills = timeoutMills - (int)(Time.unixmills() - begin);
				if (remainMills < 0) {
					throw new Exception("read timeout");
				}
			}

			await EnsureTmpCanRead(remainMills);

			var rl = tmp.Reader.Read(tmpReadBuf);
			if (rl < 1) {
				throw new Exception("empty read from tmp");
			}

			var idx = Array.IndexOf(tmpReadBuf, target, 0, rl);
			if (idx < 0) {
				if (maxBytesSize > 0 && ms.Length + rl > maxBytesSize) {
					throw new Exception("reach max size");
				}

				ms.Write(tmpReadBuf.AsSpan()[..rl]);
				continue;
			}

			if (maxBytesSize > 0 && ms.Length + idx + 1 > maxBytesSize) {
				throw new Exception("reach max size");
			}

			ms.Write(tmpReadBuf.AsSpan()[..(idx + 1)]);
			tmp.Stream.Seek(idx - rl + 1, SeekOrigin.Current);
			break;
		}
	}

	public Task<string> ReadLine(int capHit, int timeoutMills = 0, int maxBytesSize = 0) {
		return ReadLine(new MemoryStream(capHit), timeoutMills, maxBytesSize);
	}

	public async Task<string> ReadLine(MemoryStream dst, int timeoutMills = 0, int maxBytesSize = 0) {
		await ReadUntil(dst, (byte)'\n', timeoutMills: timeoutMills, maxBytesSize: maxBytesSize);
		return Encoding.UTF8.GetString(dst.GetBuffer().AsSpan()[..(int)dst.Position]);
	}

	public Task<int> Read(byte[] buf, int timeoutMills) {
		if (tmp.Stream.Position >= tmp.Stream.Length) {
			return src.Read(buf, timeoutMills);
		}

		return Task.FromResult(tmp.Reader.Read(buf));
	}

	public Task<int> Read(Memory<byte> buf, int timeoutMills) {
		if (tmp.Stream.Position >= tmp.Stream.Length) {
			return src.Read(buf, timeoutMills);
		}

		return Task.FromResult(tmp.Reader.Read(buf.Span));
	}

	public Task ReadExactly(byte[] buf, int timeoutMills) {
		if (tmp.Stream.Position >= tmp.Stream.Length) {
			return src.ReadExactly(buf, timeoutMills);
		}

		var rl = tmp.Reader.Read(buf);
		if (rl == buf.Length) return Task.CompletedTask;
		return src.ReadExactly(buf.AsMemory()[rl..], timeoutMills);
	}

	public Task ReadExactly(Memory<byte> buf, int timeoutMills) {
		if (tmp.Stream.Position >= tmp.Stream.Length) {
			return src.ReadExactly(buf, timeoutMills);
		}

		var rl = tmp.Reader.Read(buf.Span);
		if (rl == buf.Length) return Task.CompletedTask;
		return src.ReadExactly(buf[rl..], timeoutMills);
	}

	public async Task<byte> ReadByte(int timeoutMills) {
		var b = new byte[1];
		await ReadExactly(b, timeoutMills);
		return b[0];
	}

	public async Task<int> ReadAtLeast(Memory<byte> buf, int timeoutMills, int minimumBytes, bool throwWhenEnd = true) {
		if (tmp.Stream.Position >= tmp.Stream.Length) {
			return await src.ReadAtLeast(
				buf,
				timeoutMills: timeoutMills,
				minimumBytes: minimumBytes,
				throwWhenEnd: throwWhenEnd
			);
		}

		var rl = tmp.Reader.Read(buf.Span);
		if (rl >= minimumBytes) return rl;
		var nrl = await src.ReadAtLeast(
			buf[rl..],
			timeoutMills: timeoutMills,
			minimumBytes: minimumBytes - rl,
			throwWhenEnd: throwWhenEnd
		);
		return nrl + rl;
	}

	public async Task<int> ReadAtLeast(byte[] buf, int timeoutMills, int minimumBytes, bool throwWhenEnd = true) {
		if (tmp.Stream.Position >= tmp.Stream.Length) {
			return await src.ReadAtLeast(
				buf,
				timeoutMills: timeoutMills,
				minimumBytes: minimumBytes,
				throwWhenEnd: throwWhenEnd
			);
		}

		var rl = tmp.Reader.Read(buf);
		if (rl >= minimumBytes) return rl;
		var nrl = await src.ReadAtLeast(
			buf.AsMemory()[rl..],
			timeoutMills: timeoutMills,
			minimumBytes: minimumBytes - rl,
			throwWhenEnd: throwWhenEnd
		);
		return nrl + rl;
	}
}