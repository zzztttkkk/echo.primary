namespace echo.primary.core.h2tp;

public interface IHandler {
	public delegate Task HandleFunc(RequestCtx ctx);

	public Task Handle(RequestCtx ctx);
}

public class HelloWorldHandler : IHandler {
	public Task Handle(RequestCtx ctx) {
		ctx.Response.Write("Hello World\r\n");
		return Task.CompletedTask;
	}
}

public interface IMiddleware {
	public delegate Task NextFunc();

	public Task Handle(RequestCtx ctx, NextFunc? next = null);
}

public interface IRouter {
	public void Use(IMiddleware middleware, string name = "", int priority = -1);

	public IHandler? Match(string method, Uri uri);
}