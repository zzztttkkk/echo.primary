using System.Text;
using echo.primary.core.io;
using echo.primary.core.net;
using echo.primary.utils;

namespace echo.primary.core.h2tp;

public partial class RequestCtx {
	internal CancellationToken? CancellationToken = null;
	internal TcpConnection TcpConnection = null!;
	internal ConnHandleFunc? HijackFunc;

	private readonly StringBuilder _respWriteBuf = new(1024);
	public Request Request { get; } = new();
	public Response Response { get; } = new();

	private static readonly ReadOnlyMemory<byte> NewLine = "\r\n"u8.ToArray();
	private static readonly ReadOnlyMemory<byte> ChunkedEnding = "0\r\n\r\n"u8.ToArray();
	public bool IsCancellationRequested => CancellationToken is { IsCancellationRequested: true };

	internal void Reset() {
		Request.Reset();
		Response.Reset();
		_respWriteBuf.Clear();
	}

	public bool KeepAlive {
		get {
			var reqVer = Request.ProtocolVersion;
			if (!reqVer.StartsWith("HTTP/")) return false;
			var parts = reqVer[5..].Split('.');
			if (parts.Length != 2) return false;
			if (!int.TryParse(parts[0], out var mv)) return false;
			if (!int.TryParse(parts[1], out var sv)) return false;
			if (mv < 1 || sv < 1) return false;

			var cv = Response.Headers.GetLast(RfcHeader.Connection);
			return cv == null || !cv.Equals("close", StringComparison.CurrentCultureIgnoreCase);
		}
	}

	private async Task SendResponseHeader(IAsyncWriter writer) {
		var version = string.IsNullOrEmpty(Response.Flps[0]) ? "HTTP/1.1" : Response.Flps[0];
		var code = string.IsNullOrEmpty(Response.Flps[1]) ? "200" : Response.Flps[1];
		var txt = string.IsNullOrEmpty(Response.Flps[2]) ? "OK" : Response.Flps[2];

		Response.Headers.Set(RfcHeader.Date, DateTime.Now.ToString("R"));

		_respWriteBuf.Append($"{version} {code} {txt}\r\n");
		Response.Herders?.Each((k, lst) => {
			foreach (var v in lst) {
				_respWriteBuf.Append(k);
				_respWriteBuf.Append(": ");
				_respWriteBuf.Append(v);
				_respWriteBuf.Append("\r\n");
			}
		});
		_respWriteBuf.Append("\r\n");
		await writer.Write(Encoding.Latin1.GetBytes(_respWriteBuf.ToString()));
	}

	private partial Task SendFileRefResponse(IAsyncWriter writer);

	internal async Task SendResponse(IAsyncWriter writer) {
		if (IsCancellationRequested) return;

		if (Response.BodyType != BodyType.None && string.IsNullOrEmpty(Response.Headers.ContentType)) {
			Response.Headers.ContentType = Response.BodyType switch {
				BodyType.PlainText => "text/plain",
				BodyType.Binary => Mime.DefaultMimeType,
				BodyType.Json => "application/json",
				BodyType.File => Mime.GetMimeType(Response.FileRef!.Filename),
				_ => null
			};
		}

		if (Response.FileRef != null) {
			Response.EnsureWriteStream();
			await SendFileRefResponse(writer);
			return;
		}

		if (Response.Stream != null) {
			Response.EnsureWriteStream();
			await SendChunkedStreamResponse(writer, Response.Stream);
			return;
		}

		if (Response.CompressStream != null) {
			await Response.CompressStream.FlushAsync();
		}

		Response.Headers.ContentLength = Response.Body.Position;

		await SendResponseHeader(writer);

		if (Response.Body.Position > 0) {
			await writer.Write(Response.BodyBuffer);
		}

		await writer.Flush();
	}

	public delegate void ConnHandleFunc(TcpConnection connection, ExtAsyncReader reader, MemoryStream tmp);

	void Hijack(ConnHandleFunc func) {
		if (HijackFunc != null) throw new Exception("this http connection has been hijacked");
		HijackFunc = func;
	}

	void Close(bool immediately = false, string msg = $"force close by {nameof(RequestCtx)}.{nameof(Close)}") {
		if (HijackFunc != null) throw new Exception("can not close a hijacked http connection");
		if (immediately) {
			TcpConnection.Close(new Exception(msg));
			return;
		}

		Response.Headers.Set(RfcHeader.Connection, "close");
	}
}