using System.Text;
using echo.primary.core.io;
using echo.primary.utils;

namespace echo.primary.core.h2tp;

public class RequestCtx {
	internal CancellationToken? _cancellationToken = null;
	internal bool _handleTimeout = false;
	private readonly StringBuilder _respWriteBuf = new(1024);
	public Request Request { get; } = new();
	public Response Response { get; } = new();

	public static int StreamReadBufSize = 4096;
	private static readonly byte[] NewLine = "\r\n"u8.ToArray();
	private static readonly byte[] ChunkedEndding = "0\r\n\r\n"u8.ToArray();

	internal void Reset() {
		Request.Reset();
		Response.Reset();
		_respWriteBuf.Clear();
	}

	private async Task SendResponseHeader(IAsyncWriter writer) {
		var version = string.IsNullOrEmpty(Response.flps[0]) ? "HTTP/1.1" : Response.flps[0];
		var code = string.IsNullOrEmpty(Response.flps[1]) ? "200" : Response.flps[1];
		var txt = string.IsNullOrEmpty(Response.flps[2]) ? "OK" : Response.flps[2];
		_respWriteBuf.Append($"{version} {code} {txt}\r\n");
		Response.herders?.Each((k, lst) => {
			foreach (var v in lst) {
				_respWriteBuf.Append($"{k}: {v}\r\n");
			}
		});
		_respWriteBuf.Append("\r\n");
		await writer.Write(Encoding.Latin1.GetBytes(_respWriteBuf.ToString()));
	}

	private async Task SendRangeFileRefResponse(IAsyncWriter writer, long filesize, byte[] tmp) {
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
		if (remain <= StreamReadBufSize) {
			var buf = tmp.AsMemory();
			var rl = await fs.ReadAsync(buf);
			if (rl != remain) {
				throw new Exception("");
			}

			await SendSmallBytesResponse(writer, buf[..rl]);
			return;
		}

		await SendSizedChunkedStreamResponse(writer, fs, tmp, remain);
	}

	private async Task SendSmallBytesResponse(IAsyncWriter writer, ReadOnlyMemory<byte> bytes) {
	}

	private async Task SendSizedChunkedStreamResponse(IAsyncWriter writer, Stream stream, byte[] tmp, long remain) {
		if (remain < 1) return;
		if (Response._compressStream != null) {
			await _SendSizedChunkedStreamWithCompresion(writer, stream, tmp, remain);
			return;
		}

		Response.Headers.Set(HttpRfcHeader.TransferEncoding, "chunked");
		await SendResponseHeader(writer);

		while (true) {
			var buf = tmp.AsMemory();
			if (remain < StreamReadBufSize) {
				buf = buf[..(int)remain];
			}

			var rl = await stream.ReadAsync(buf);
			if (rl == 0) {
				throw new Exception("read failed");
			}

			remain -= rl;
			if (remain <= 0) {
				await writer.Write(ChunkedEndding);
				break;
			}

			await writer.Write(Encoding.ASCII.GetBytes($"{rl}\r\n"));
			await writer.Write(buf[..rl]);
			await writer.Write(NewLine);
		}

		await writer.Flush();
	}

	private async Task SendChunkedStreamResponse(IAsyncWriter writer, Stream stream, byte[] tmp) {
		Response.Headers.Set(HttpRfcHeader.TransferEncoding, "chunked");

		if (Response._compressStream != null) {
			await _SendChunkedStreamWithCompresion(writer, stream, tmp);
			return;
		}

		var buf = tmp.AsMemory();

		while (true) {
			var rl = await stream.ReadAsync(buf);
			if (rl == 0) {
				await writer.Write(ChunkedEndding);
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
		byte[] tmp,
		long remain
	) {
		Response.Headers.Set(HttpRfcHeader.TransferEncoding, "chunked");
		await SendResponseHeader(writer);

		var cs = Response._compressStream!;
		var body = Response.body!;

		while (true) {
			var buf = tmp.AsMemory();
			if (remain < StreamReadBufSize) {
				buf = buf[..(int)remain];
			}

			var rl = await stream.ReadAsync(buf);
			if (rl == 0) {
				throw new Exception("read failed");
			}

			await cs.WriteAsync(buf[..rl]);
			if (body.Position >= StreamReadBufSize) {
				await writer.Write(Encoding.ASCII.GetBytes($"{body.Position}\r\n"));
				await writer.Write(body.ToArray().AsMemory());
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
			await writer.Write(body.ToArray().AsMemory());
			await writer.Write(NewLine);
		}

		await writer.Write(ChunkedEndding);

		await writer.Flush();
	}

	private async Task _SendChunkedStreamWithCompresion(IAsyncWriter writer, Stream stream, byte[] tmp) {
		Response.Headers.Set(HttpRfcHeader.TransferEncoding, "chunked");

		var cs = Response._compressStream!;
		var body = Response.body!;
		var buf = tmp.AsMemory();

		while (true) {
			var rl = await stream.ReadAsync(buf);

			await cs.WriteAsync(buf[..rl]);
			if (body.Position >= StreamReadBufSize) {
				await writer.Write(Encoding.ASCII.GetBytes($"{body.Position}\r\n"));
				await writer.Write(body.ToArray().AsMemory());
				await writer.Write(NewLine);
				body.Position = 0;
			}

			if (rl == 0) {
				break;
			}
		}

		if (body.Position != 0) {
			await writer.Write(Encoding.ASCII.GetBytes($"{body.Position}\r\n"));
			await writer.Write(body.ToArray().AsMemory());
			await writer.Write(NewLine);
		}

		await writer.Write(ChunkedEndding);

		await writer.Flush();
	}

	private async Task SendFileRefResponse(IAsyncWriter writer) {
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

		var tmp = new byte[StreamReadBufSize];

		if (fileRef.range != null) {
			await SendRangeFileRefResponse(writer, filesize, tmp);
			return;
		}

		if (filesize <= StreamReadBufSize) {
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
		await SendChunkedStreamResponse(writer, bigFs, tmp);
	}

	internal async Task SendResponse(IAsyncWriter writer) {
		if (_handleTimeout) return;

		if (Response._compressStream != null) {
			await Response._compressStream.FlushAsync();
		}

		if (Response.body is { Length: > 0 }) {
			Response.Headers.ContentLength = Response.body.Length;
		}

		if (Response.BodyType != BodyType.None && string.IsNullOrEmpty(Response.Headers.ContentType)) {
			Response.Headers.ContentType = Response.BodyType switch {
				BodyType.PlainText => "text/plain",
				BodyType.Binary => Mime.DefaultMimeType,
				BodyType.JSON => "application/json",
				BodyType.File => Mime.GetMimeType(Response._fileRef!.filename),
				_ => Mime.DefaultMimeType
			};
		}


		if (Response._fileRef != null) {
			await SendFileRefResponse(writer);
			return;
		}

		await SendResponseHeader(writer);

		if (Response.body is { Length: > 0 }) {
			await writer.Write(Response.body.ToArray());
			await writer.Flush();
		}
	}
}