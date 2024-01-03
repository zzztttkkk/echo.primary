using System.Text;
using echo.primary.core.io;
using echo.primary.core.net;
using echo.primary.utils;

namespace echo.primary.core.h2tp;

public partial class RequestCtx {
	internal CancellationToken? CancellationToken = null;
	internal TcpConnection TcpConnection = null!;
	internal bool Hijacked;

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

	public bool ShouldKeepAlive => true;

	private async Task SendResponseHeader(IAsyncWriter writer) {
		var version = string.IsNullOrEmpty(Response.Flps[0]) ? "HTTP/1.1" : Response.Flps[0];
		var code = string.IsNullOrEmpty(Response.Flps[1]) ? "200" : Response.Flps[1];
		var txt = string.IsNullOrEmpty(Response.Flps[2]) ? "OK" : Response.Flps[2];
		_respWriteBuf.Append($"{version} {code} {txt}\r\n");
		Response.Herders?.Each((k, lst) => {
			foreach (var v in lst) {
				_respWriteBuf.Append($"{k}: {v}\r\n");
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

	public delegate void ConnHandleFunc(TcpConnection connection, MemoryStream tmp);

	void Hijack(ConnHandleFunc handle) {
		if (Hijacked) throw new Exception("");
		Hijacked = true;
		handle(TcpConnection, ReadTmp);
	}
}