using echo.primary.core.io;

namespace echo.primary.core.h2tp;

public class MultiPart {
	private string? boundary;

	private static string GenBoundary() {
		return "";
	}

	public async Task WriteBegin(IAsyncWriter dst, string[]? headers = null) {
		boundary ??= GenBoundary();
		
	}
}