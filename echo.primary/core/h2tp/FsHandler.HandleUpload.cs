namespace echo.primary.core.h2tp;

public partial class FsHandler {
	private partial Task HandleUpload(RequestCtx ctx, string path) {
		return Task.CompletedTask;
	}
}