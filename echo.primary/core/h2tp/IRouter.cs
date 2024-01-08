namespace echo.primary.core.h2tp;

public interface IRouter {
	IHandler? Match(RequestCtx ctx);

	Task Handle(RequestCtx ctx) {
		var handler = Match(ctx);
		return handler == null ? Task.CompletedTask : handler.Handle(ctx);
	}
}

public class SimpleHashRouter : IRouter {
	private readonly Dictionary<string, IHandler> _mapping = new();

	public void Register(string path, IHandler handler) {
		_mapping[path] = handler;
	}

	public IHandler? Match(RequestCtx ctx) {
		var path = ctx.Request.Uri.Path;
		if (string.IsNullOrEmpty(path) || !path.StartsWith('/')) {
			ctx.Response.StatusCode = (int)RfcStatusCode.BadRequest;
			return null;
		}

		while (true) {
			if (_mapping.TryGetValue(path, out var handler)) {
				return handler;
			}

			path = path[..path.LastIndexOf('/')];
			if (!string.IsNullOrEmpty(path)) continue;

			ctx.Response.StatusCode = (int)RfcStatusCode.NotFound;

			return null;
		}
	}
}