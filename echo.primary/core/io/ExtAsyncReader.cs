using System.Text;
using echo.primary.utils;

namespace echo.primary.core.io;

public class ExtAsyncReader(
	IAsyncReader src,
	int tmpCap = 4096,
	int readSize = 512,
	int srcReadSize = 512
) : IAsyncReader {
	private readonly BytesBuffer tmp = new(tmpCap);
	private readonly byte[] rbuf = new byte[srcReadSize];

	private async Task EnsureTmpCanRead(int timeoutMills) {
		if (tmp.Stream.Position < tmp.Stream.Length) return;

		var rl = await src.Read(rbuf, timeoutMills);
		if (rl < 1) {
			throw new Exception("empty read from src");
		}

		tmp.Stream.Position = 0;
		tmp.Writer.Write(rbuf.AsSpan()[..rl]);
		tmp.Stream.Position = 0;
	}

	public async Task<int> Read(byte[] buf) {
		if (tmp.Stream.Position >= tmp.Stream.Length) {
			return await src.Read(buf);
		}

		return tmp.Reader.Read(buf);
	}

	public async Task<int> Read(byte[] buf, int timeoutMills) {
		if (tmp.Stream.Position >= tmp.Stream.Length) {
			return await src.Read(buf, timeoutMills);
		}

		return tmp.Reader.Read(buf);
	}

	public Task<bool> ReadExactly(byte[] buf) {
		throw new NotImplementedException();
	}

	public Task<bool> ReadExactly(byte[] buf, int timeoutMills) {
		throw new NotImplementedException();
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

			var rl = tmp.Reader.Read(rbuf);
			if (rl < 1) {
				throw new Exception("empty read from tmp");
			}

			var idx = Array.IndexOf(rbuf, target, 0, rl);
			if (idx < 0) {
				if (maxBytesSize > 0 && ms.Length + rl > maxBytesSize) {
					throw new Exception("reach max size");
				}

				ms.Write(rbuf.AsSpan()[..rl]);
				continue;
			}

			if (maxBytesSize > 0 && ms.Length + idx + 1 > maxBytesSize) {
				throw new Exception("reach max size");
			}

			ms.Write(rbuf.AsSpan()[..(idx + 1)]);
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
}