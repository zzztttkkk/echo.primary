namespace echo.primary.utils;

public static class ExceptionHelper {
	public static Exception? UnwrapFirst(Exception? exception) {
		if (exception == null) return null;
		return exception switch {
			AggregateException ae => ae.InnerException,
			_ => exception
		};
	}
}