using echo.primary.core.io;

namespace echo.primary.core.h2tp;

public class RequestCtx {
	internal CancellationToken? _cancellationToken = null;
	internal bool _handleTimeout = false;
	public Request Request { get; } = new();
	public Response Response { get; } = new();

	internal void Reset() {
		Request.Reset();
		Response.Reset();
	}

	internal async Task SendResponse(IAsyncWriter writer) {
		if (_handleTimeout) return;
		if (Response.body is { Length: > 0 }) {
			Response.Headers.ContentLength = Response.body.Length;
		}
	}
}