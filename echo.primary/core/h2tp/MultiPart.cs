using echo.primary.core.io;

namespace echo.primary.core.h2tp;

public class MultiPart {
	private string? boundary;

	private static string GenBoundary() {
		return "";
	}

	public delegate void WriteLineCall();

	public async Task WriteHeader(IAsyncWriter dst, string[]? headers = null) {
		boundary ??= GenBoundary();
	}
}