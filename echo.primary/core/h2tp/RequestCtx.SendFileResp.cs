using System.Text;
using echo.primary.core.io;

namespace echo.primary.core.h2tp;

public partial class RequestCtx {
	internal byte[] tmp = null!;

	private async Task SendRangeFileRefResponse(IAsyncWriter writer, long filesize) {
		var fileRef = Response._fileRef!;
		var range = fileRef.range!;

		var begin = range.Item1;
		if (begin < 0) begin = 0;
		var end = range.Item2;

		if (end < begin || end > filesize) {
			throw new Exception($"bad file range, end {end} > filesize {filesize}, {fileRef.filename}");
		}

		Response.Headers.Set(HttpRfcHeader.ContentRange, $"bytes {begin}-{end}/{filesize}");

		await using var fs = fileRef.fileinfo.OpenRead();
		fs.Seek(begin, SeekOrigin.Begin);

		var remain = begin - end + 1;
		if (remain <= tmp.Length) {
			var buf = tmp.AsMemory();
			var rl = await fs.ReadAsync(buf);
			if (rl != remain) {
				throw new Exception("");
			}

			await SendSmallBytesResponse(writer, buf[..rl]);
			return;
		}

		await SendSizedChunkedStreamResponse(writer, fs, remain);
	}

	private async Task SendSmallBytesResponse(IAsyncWriter writer, ReadOnlyMemory<byte> bytes) {
	}

	private async Task SendSizedChunkedStreamResponse(IAsyncWriter writer, Stream stream, long remain) {
		if (remain < 1) return;
		if (Response._compressStream != null) {
			await _SendSizedChunkedStreamWithCompresion(writer, stream, remain);
			return;
		}

		Response.Headers.Set(HttpRfcHeader.TransferEncoding, "chunked");
		await SendResponseHeader(writer);

		while (true) {
			var buf = tmp.AsMemory();
			if (remain < tmp.Length) {
				buf = buf[..(int)remain];
			}

			var rl = await stream.ReadAsync(buf);
			if (rl == 0) {
				throw new Exception("read failed");
			}

			remain -= rl;
			if (remain <= 0) {
				await writer.Write(ChunkedEnding);
				break;
			}

			await writer.Write(Encoding.ASCII.GetBytes($"{rl}\r\n"));
			await writer.Write(buf[..rl]);
			await writer.Write(NewLine);
		}

		await writer.Flush();
	}

	private async Task SendChunkedStreamResponse(IAsyncWriter writer, Stream stream) {
		Response.Headers.Set(HttpRfcHeader.TransferEncoding, "chunked");

		if (Response._compressStream != null) {
			await _SendChunkedStreamWithCompresion(writer, stream);
			return;
		}

		var buf = tmp.AsMemory();

		while (true) {
			var rl = await stream.ReadAsync(buf);
			if (rl == 0) {
				await writer.Write(ChunkedEnding);
				break;
			}

			await writer.Write(Encoding.ASCII.GetBytes($"{rl}\r\n"));
			await writer.Write(buf[..rl]);
			await writer.Write(NewLine);
		}

		await writer.Flush();
	}

	private async Task _SendSizedChunkedStreamWithCompresion(
		IAsyncWriter writer,
		Stream stream,
		long remain
	) {
		Response.Headers.Set(HttpRfcHeader.TransferEncoding, "chunked");
		await SendResponseHeader(writer);

		var cs = Response._compressStream!;
		var body = Response.body!;

		while (true) {
			var buf = tmp.AsMemory();
			if (remain < tmp.Length) {
				buf = buf[..(int)remain];
			}

			var rl = await stream.ReadAsync(buf);
			if (rl == 0) {
				throw new Exception("read failed");
			}

			await cs.WriteAsync(buf[..rl]);
			if (body.Position >= tmp.Length) {
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

	private async Task _SendChunkedStreamWithCompresion(IAsyncWriter writer, Stream stream) {
		Response.Headers.Set(HttpRfcHeader.TransferEncoding, "chunked");

		var cs = Response._compressStream!;
		var body = Response.body!;
		var buf = tmp.AsMemory();

		while (true) {
			var rl = await stream.ReadAsync(buf);

			await cs.WriteAsync(buf[..rl]);
			if (body.Position >= tmp.Length) {
				await writer.Write(Encoding.ASCII.GetBytes($"{body.Position}\r\n"));
				await writer.Write(body.GetBuffer().AsMemory()[..rl]);
				await writer.Write(NewLine);
				body.Position = 0;
			}

			if (rl == 0) {
				break;
			}
		}

		if (body.Position != 0) {
			await writer.Write(Encoding.ASCII.GetBytes($"{body.Position}\r\n"));
			await writer.Write(body.GetBuffer().AsMemory()[..(int)(body.Position)]);
			await writer.Write(NewLine);
		}

		await writer.Write(ChunkedEnding);

		await writer.Flush();
	}

	private async partial Task SendFileRefResponse(IAsyncWriter writer) {
		var fileRef = Response._fileRef!;
		var filesize = await Task.Run(() => fileRef.fileinfo.Length);

		if (fileRef.range == null && fileRef.viaSendFile) {
			Response.Headers.ContentLength = filesize;
			Response.Headers.Del(HttpRfcHeader.ContentEncoding);
			await SendResponseHeader(writer);
			await writer.Flush();
			await writer.SendFile(fileRef.filename);
			return;
		}

		if (fileRef.range != null) {
			await SendRangeFileRefResponse(writer, filesize);
			return;
		}

		if (filesize <= tmp.Length) {
			await using var smallFs = fileRef.fileinfo.OpenRead();

			var rbuf = tmp.AsMemory()[..(int)filesize];
			var rl = await smallFs.ReadAsync(rbuf);
			if (rl != filesize) {
				throw new IOException("read failed");
			}

			await SendSmallBytesResponse(writer, rbuf[..rl]);
			return;
		}

		Response.Headers.Set(HttpRfcHeader.TransferEncoding, "chunked");
		await SendResponseHeader(writer);

		await using var bigFs = fileRef.fileinfo.OpenRead();
		await SendChunkedStreamResponse(writer, bigFs);
	}
}