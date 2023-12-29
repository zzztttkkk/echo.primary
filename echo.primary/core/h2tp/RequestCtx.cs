using System.Text;
using echo.primary.core.io;
using echo.primary.core.net;
using echo.primary.utils;

namespace echo.primary.core.h2tp;

public partial class RequestCtx {
	internal CancellationToken? CancellationToken = null;
	internal bool HandleTimeout = false;
	internal TcpConnection TcpConnection = null!;
	internal bool Hijacked;

	private readonly StringBuilder _respWriteBuf = new(1024);
	public Request Request { get; } = new();
	public Response Response { get; } = new();

	private static readonly byte[] NewLine = "\r\n"u8.ToArray();
	private static readonly byte[] ChunkedEnding = "0\r\n\r\n"u8.ToArray();

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
		if (HandleTimeout) return;

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

		if (Response._stream != null) {
			await SendChunkedStreamResponse(writer, Response._stream);
			return;
		}

		if (Response._compressStream != null) {
			await Response._compressStream.FlushAsync();
		}

		if (Response.Body is { Length: > 0 }) {
			Response.Headers.ContentLength = Response.Body.Length;
		}

		await SendResponseHeader(writer);

		if (Response.Body is { Length: > 0 }) {
			await writer.Write(Response.Body.GetBuffer().AsMemory()[..(int)Response.Body.Position]);
			await writer.Flush();
		}
	}

	public delegate void ConnHandleFunc(TcpConnection connection, MemoryStream tmp);

	void Hijack(ConnHandleFunc handle) {
		if (Hijacked) throw new Exception("");
		Hijacked = true;
		handle(TcpConnection, ReadTmp);
	}
}