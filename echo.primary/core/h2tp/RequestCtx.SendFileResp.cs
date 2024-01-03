using System.Text;
using echo.primary.core.io;
using echo.primary.utils;

namespace echo.primary.core.h2tp;

public partial class RequestCtx {
	internal MemoryStream ReadTmp = null!;

	private async Task SendRangeFileRefResponse(IAsyncWriter writer, long filesize) {
		var fileRef = Response.FileRef!;
		var range = fileRef.Range!;

		var (begin, end) = range;
		if (begin < 0) begin = 0;
		if (end < begin || end > filesize) {
			throw new Exception($"bad file range, end {end} > filesize {filesize}, {fileRef.Filename}");
		}

		Response.Headers.Set(RfcHeader.ContentRange, $"bytes {begin}-{end}/{filesize}");

		await using var fs = fileRef.FileInfo.OpenRead();
		fs.Seek(begin, SeekOrigin.Begin);

		var remain = begin - end + 1;
		if (remain <= ReadTmp.Length) {
			var buf = ReadTmp.GetBuffer().AsMemory();
			var rl = await fs.ReadAsync(buf);
			if (rl != remain) {
				throw new Exception("");
			}

			await SendSmallResponse(writer, buf[..rl]);
			return;
		}

		await SendSizeLimitedChunkedStreamResponse(writer, fs, remain);
	}

	private async Task SendSmallResponse(IAsyncWriter writer, ReadOnlyMemory<byte> bytes) {
		Response.Write(bytes);
		if (Response.CompressStream != null) {
			await Response.CompressStream.FlushAsync();
		}

		Response.Headers.ContentLength = Response.Body.Position;
		await SendResponseHeader(writer);
		await writer.Write(Response.BodyBuffer);
	}

	private async Task SendSizeLimitedChunkedStreamResponse(IAsyncWriter writer, Stream stream, long remain) {
		if (remain < 1) return;
		if (Response.CompressStream != null) {
			await _SendSizedChunkedStreamWithCompression(writer, stream, remain);
			return;
		}

		Response.Headers.Set(RfcHeader.TransferEncoding, "chunked");
		await SendResponseHeader(writer);

		while (true) {
			var buf = ReadTmp.GetBuffer().AsMemory();
			if (remain < ReadTmp.Length) {
				buf = buf[..(int)remain];
			}

			var rl = await stream.ReadAsync(buf);
			if (rl == 0) {
				throw new EndOfStreamException();
			}

			remain -= rl;
			if (remain <= 0) {
				await writer.Write(ChunkedEnding);
				break;
			}

			await WriteChunkLength(writer, rl);
			await writer.Write(buf[..rl]);
			await writer.Write(NewLine);
		}

		await writer.Flush();
	}

	private static Task WriteChunkLength(IAsyncWriter writer, int len) {
		// ReSharper disable once UseCollectionExpression, RedundantExplicitArraySize
		var lentmp = new byte[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
		var tmp = Hex.ToBytes((uint)len);
		var tmplen = tmp.Length;
		Array.Copy(tmp, lentmp, tmplen);

		lentmp[tmplen] = (byte)'\r';
		lentmp[tmplen + 1] = (byte)'\n';
		return writer.Write(lentmp[..(tmplen + 2)]);
	}

	private async Task SendChunkedStreamResponse(IAsyncWriter writer, Stream stream) {
		Response.Headers.Set(RfcHeader.TransferEncoding, "chunked");
		await SendResponseHeader(writer);

		if (Response.CompressStream != null) {
			await _SendChunkedStreamWithCompression(writer, stream);
			return;
		}

		var buf = ReadTmp.GetBuffer().AsMemory();

		while (true) {
			var rl = await stream.ReadAsync(buf);
			if (rl == 0) {
				await writer.Write(ChunkedEnding);
				break;
			}

			await WriteChunkLength(writer, rl);
			await writer.Write(buf[..rl]);
			await writer.Write(NewLine);
		}

		await writer.Flush();
	}

	private async Task _SendSizedChunkedStreamWithCompression(
		IAsyncWriter writer,
		Stream stream,
		long remain
	) {
		Response.Headers.Set(RfcHeader.TransferEncoding, "chunked");
		await SendResponseHeader(writer);

		var cs = Response.CompressStream!;
		var body = Response.Body;

		while (true) {
			var buf = ReadTmp.GetBuffer().AsMemory();
			if (remain < ReadTmp.Length) {
				buf = buf[..(int)remain];
			}

			var rl = await stream.ReadAsync(buf);
			if (rl == 0) {
				throw new Exception("read failed");
			}

			await cs.WriteAsync(buf[..rl]);
			if (body.Position >= ReadTmp.Length) {
				await writer.Write(Encoding.ASCII.GetBytes($"{body.Position}\r\n"));
				await writer.Write(body.GetBuffer().AsMemory()[..rl]);
				await writer.Write(NewLine);
				body.Position = 0;
			}

			remain -= rl;
			if (remain <= 0) {
				break;
			}
		}

		if (body.Position != 0) {
			await writer.Write(Encoding.ASCII.GetBytes($"{body.Position}\r\n"));
			await writer.Write(body.GetBuffer().AsMemory()[..(int)body.Position]);
			await writer.Write(NewLine);
		}

		await writer.Write(ChunkedEnding);

		await writer.Flush();
	}

	private async Task _SendChunkedStreamWithCompression(IAsyncWriter writer, Stream stream) {
		var cs = Response.CompressStream!;
		var body = Response.Body;
		var buf = ReadTmp.GetBuffer().AsMemory();

		while (true) {
			var rl = await stream.ReadAsync(buf);

			await cs.WriteAsync(buf[..rl]);
			if (body.Position >= ReadTmp.Length) {
				await WriteChunkLength(writer, (int)body.Position);
				await writer.Write(Response.BodyBuffer);
				await writer.Write(NewLine);
				body.Position = 0;
			}

			if (rl == 0) {
				break;
			}
		}

		await cs.FlushAsync();

		if (body.Position != 0) {
			await WriteChunkLength(writer, (int)body.Position);
			await writer.Write(Response.BodyBuffer);
			await writer.Write(NewLine);
		}

		await writer.Write(ChunkedEnding);

		await writer.Flush();
	}

	private async partial Task SendFileRefResponse(IAsyncWriter writer) {
		var fileRef = Response.FileRef!;
		var filesize = await Task.Run(() => fileRef.FileInfo.Length);

		if (fileRef.Range == null && fileRef.ViaSendFile) {
			Response.Headers.ContentLength = filesize;
			await SendResponseHeader(writer);
			await writer.Flush();
			await writer.SendFile(fileRef.Filename);
			return;
		}

		if (fileRef.Range != null) {
			await SendRangeFileRefResponse(writer, filesize);
			return;
		}

		var tmp = ReadTmp.GetBuffer().AsMemory();

		if (filesize <= ReadTmp.Length) {
			await using var smallfs = fileRef.FileInfo.OpenRead();

			var rbuf = tmp[..(int)filesize];
			var rl = await smallfs.ReadAsync(rbuf);
			if (rl != filesize) {
				throw new IOException("read failed");
			}

			await SendSmallResponse(writer, rbuf[..rl]);
			return;
		}

		await using var bigfs = fileRef.FileInfo.OpenRead();
		await SendChunkedStreamResponse(writer, bigfs);
	}
}