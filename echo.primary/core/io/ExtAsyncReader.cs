﻿using System.Text;
using echo.primary.utils;

namespace echo.primary.core.io;

public class ExtAsyncReader(IAsyncReader src, BytesBuffer tmp) : IAsyncReader {
	private readonly byte[] _tmpReadBuf = GC.AllocateUninitializedArray<byte>(512); // read from tmp
	private readonly byte[] _srcReadBuf = GC.AllocateUninitializedArray<byte>(4096); // read from src
	private bool TmpCanRead => tmp.Stream.Position < tmp.Stream.Length;

	private async Task EnsureTmpCanRead(int timeoutMills) {
		if (TmpCanRead) return;

		var rl = await src.Read(_srcReadBuf, timeoutMills);
		if (rl < 1) {
			throw new EndOfStreamException();
		}

		tmp.Stream.Position = 0;
		tmp.Writer.Write(_srcReadBuf.AsSpan()[..rl]);
		tmp.Stream.Position = 0;
	}

	public async Task ReadUntil(MemoryStream ms, byte target, int timeoutMills = 0, int maxBytesSize = 0) {
		ms.Position = 0;

		var begin = Time.Unixmills();
		while (true) {
			var remainMills = -1;
			if (timeoutMills > 0) {
				remainMills = timeoutMills - (int)(Time.Unixmills() - begin);
				if (remainMills < 0) {
					throw new IOException("read timeout");
				}
			}

			await EnsureTmpCanRead(remainMills);

			var rl = tmp.Reader.Read(_tmpReadBuf);
			if (rl < 1) {
				throw new EndOfStreamException();
			}

			var idx = ((ReadOnlySpan<byte>)_tmpReadBuf.AsSpan(0, rl)).IndexOf(target);
			if (idx < 0) {
				if (maxBytesSize > 0 && ms.Length + rl > maxBytesSize) {
					throw new IOException("reach max size");
				}

				ms.Write(_tmpReadBuf.AsSpan()[..rl]);
				continue;
			}

			if (maxBytesSize > 0 && ms.Length + idx + 1 > maxBytesSize) {
				throw new IOException("reach max size");
			}

			ms.Write(_tmpReadBuf.AsSpan()[..(idx + 1)]);
			tmp.Stream.Seek(idx - rl + 1, SeekOrigin.Current);
			break;
		}
	}

	public async Task<string> ReadLine(
		MemoryStream dst,
		int timeoutMills = 0,
		int maxBytesSize = 0,
		Encoding? encoding = null
	) {
		await ReadUntil(dst, (byte)'\n', timeoutMills: timeoutMills, maxBytesSize: maxBytesSize);
		return (encoding ?? Encoding.UTF8).GetString(dst.GetBuffer().AsSpan()[..(int)dst.Position]);
	}


	public Task<int> Read(byte[] buf, int timeoutMills) {
		return !TmpCanRead ? src.Read(buf, timeoutMills) : Task.FromResult(tmp.Reader.Read(buf));
	}

	public Task<int> Read(Memory<byte> buf, int timeoutMills) {
		return !TmpCanRead ? src.Read(buf, timeoutMills) : Task.FromResult(tmp.Reader.Read(buf.Span));
	}

	public Task ReadExactly(byte[] buf, int timeoutMills) {
		if (!TmpCanRead) {
			return src.ReadExactly(buf, timeoutMills);
		}

		var rl = tmp.Reader.Read(buf);
		return rl == buf.Length ? Task.CompletedTask : src.ReadExactly(buf.AsMemory()[rl..], timeoutMills);
	}

	public Task ReadExactly(Memory<byte> buf, int timeoutMills) {
		if (!TmpCanRead) {
			return src.ReadExactly(buf, timeoutMills);
		}

		var rl = tmp.Reader.Read(buf.Span);
		return rl == buf.Length ? Task.CompletedTask : src.ReadExactly(buf[rl..], timeoutMills);
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