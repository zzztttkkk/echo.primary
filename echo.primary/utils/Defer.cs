namespace echo.primary.utils;

public class Defer(Action action) : IDisposable {
	public void Dispose() {
		action();
		GC.SuppressFinalize(this);
	}
}