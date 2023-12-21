using System.Text;
using echo.primary.utils;

namespace echo.primary.core.io;

public class ExtAsyncReader(IAsyncReader src, int tmpcap = 4096, int readsize = 512) {
	private readonly BytesBuffer tmp = new(tmpcap);
	private readonly byte[] rbuf = new byte[readsize];

	private async Task EnsureTmpCanRead(int timeoutmills) {
		if (tmp.Stream.Position >= tmp.Stream.Length) return;


		var rl = await src.Read(rbuf, timeoutmills);
		if (rl < 1) {
			throw new Exception("empty read from src");
		}

		tmp.Stream.Position = 0;
		tmp.Writer.Write(rbuf.AsSpan()[..rl]);
		tmp.Stream.Position = 0;
	}

	public async Task<int> Read(byte[] buf, int timeoutmills = 0) {
		if (tmp.Stream.Position >= tmp.Stream.Length) {
			return await src.Read(buf, timeoutmills);
		}

		return tmp.Reader.Read(buf);
	}

	public async Task ReadUntil(MemoryStream ms, byte target, int timeoutmills = 0, int maxbytessize = 0) {
		var begin = Time.unixmills();
		while (true) {
			var remainmills = -1;
			if (timeoutmills > 0) {
				remainmills = timeoutmills - (int)(Time.unixmills() - begin);
				if (remainmills < 0) {
					throw new Exception("read timeout");
				}
			}

			await EnsureTmpCanRead(remainmills);

			var rl = tmp.Reader.Read(rbuf);
			if (rl < 1) {
				throw new Exception("empty read from tmp");
			}

			var idx = Array.IndexOf(rbuf, target, 0, rl);
			if (idx < 0) {
				if (maxbytessize > 0 && ms.Length + rl > maxbytessize) {
					throw new Exception("reach max size");
				}

				ms.Write(rbuf.AsSpan()[..rl]);
				continue;
			}

			if (maxbytessize > 0 && ms.Length + idx + 1 > maxbytessize) {
				throw new Exception("reach max size");
			}

			ms.Write(rbuf.AsSpan()[..(idx + 1)]);
			tmp.Stream.Seek(-idx, SeekOrigin.Current);
			break;
		}
	}

	public Task<string> ReadLine(int caphit, int timeoutmills = 0, int maxbytessize = 0) {
		return ReadLine(new MemoryStream(caphit), timeoutmills, maxbytessize);
	}

	public async Task<string> ReadLine(MemoryStream ms, int timeoutmills = 0, int maxbytessize = 0) {
		ms.Position = 0;
		await ReadUntil(ms, (byte)'\n', timeoutmills: timeoutmills, maxbytessize: maxbytessize);
		return Encoding.UTF8.GetString(ms.GetBuffer().AsSpan()[..(int)ms.Position]);
	}
}