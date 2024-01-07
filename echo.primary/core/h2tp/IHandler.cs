namespace echo.primary.core.h2tp;

public interface IHandler {
	public delegate Task HandleFunc(RequestCtx ctx);

	public Task Handle(RequestCtx ctx);
}

public class HelloWorldHandler : IHandler {
	public Task Handle(RequestCtx ctx) {
		ctx.Response.NoCompression = true;
		ctx.Response.Write("Hello World\r\n");
		return Task.CompletedTask;
	}
}