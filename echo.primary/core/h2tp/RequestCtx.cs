using System.Text;
using echo.primary.core.io;

namespace echo.primary.core.h2tp;

public class RequestCtx {
	internal CancellationToken? _cancellationToken = null;
	internal bool _handleTimeout = false;
	private readonly StringBuilder _respWriteBuf = new(1024);
	public Request Request { get; } = new();
	public Response Response { get; } = new();

	internal void Reset() {
		Request.Reset();
		Response.Reset();
		_respWriteBuf.Clear();
	}

	internal async Task SendResponse(IAsyncWriter writer) {
		if (_handleTimeout) return;
		if (Response._compressStream != null) {
			await Response._compressStream.FlushAsync();
		}

		if (Response.body is { Length: > 0 }) {
			Response.Headers.ContentLength = Response.body.Length;
		}
		else if (Response._fileRef != null) {
			if (Response._fileRef.range != null) {
				// todo range file
			}

			Response.Headers.ContentLength = await Task.Run(() => Response._fileRef!.fileinfo!.Length);
		}

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

		if (Response.body is { Length: > 0 }) {
			await writer.Write(Response.body.ToArray());
			await writer.Flush();
		}
		else if (Response._fileRef != null) {
			await writer.Flush();
			await writer.SendFile(Response._fileRef.fileinfo!.FullName);
		}
	}
}