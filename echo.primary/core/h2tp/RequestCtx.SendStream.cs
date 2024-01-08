using echo.primary.core.io;
using echo.primary.utils;

namespace echo.primary.core.h2tp;

public partial class RequestCtx {
	internal MemoryStream ReadTmp = null!;

	private async Task SendSmallBytes(IAsyncWriter writer, ReadOnlyMemory<byte> bytes) {
		Response.WriteBytes(bytes.Span);
		if (Response.CompressStream != null) {
			await Response.CompressStream.FlushAsync();
		}

		Response.Headers.ContentLength = Response.Body.Position;
		await SendHeader(writer);
		await writer.Write(Response.BodyBuffer);
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

	private partial async Task SendStream(IAsyncWriter writer, Stream stream) {
		Response.Headers.Set(RfcHeader.TransferEncoding, "chunked");
		await SendHeader(writer);

		if (Response.CompressStream != null) {
			await _SendStreamWithCompression(writer, stream);
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

	private async Task _SendStreamWithCompression(IAsyncWriter writer, Stream stream) {
		var cs = Response.CompressStream!;
		var body = Response.Body;
		var buf = ReadTmp.GetBuffer().AsMemory();

		while (true) {
			var rl = await stream.ReadAsync(buf);

			await cs.WriteAsync(buf[..rl]);
			if (body.Position >= ReadTmp.Capacity) {
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

	private async partial Task SendFileRef(IAsyncWriter writer) {
		var fileRef = (FileRef)Response.BodyRef.Value!;
		var filesize = fileRef.FileInfo.Length;

		if (fileRef.ViaSendFile) {
			Response.Headers.ContentLength = filesize;
			await SendHeader(writer);
			await writer.Flush();
			await writer.SendFile(fileRef.Filename);
			return;
		}

		var tmp = ReadTmp.GetBuffer().AsMemory();

		if (filesize <= ReadTmp.Capacity) {
			await using var smallfs = fileRef.FileInfo.OpenRead();

			var rbuf = tmp[..(int)filesize];
			var rl = await smallfs.ReadAsync(rbuf);
			if (rl != filesize) {
				throw new IOException("read failed");
			}

			await SendSmallBytes(writer, rbuf[..rl]);
			return;
		}

		await using var bigfs = fileRef.FileInfo.OpenRead();
		await SendStream(writer, bigfs);
	}
}