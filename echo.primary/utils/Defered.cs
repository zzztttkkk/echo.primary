namespace echo.primary.utils;

public class Defered(Action action) : IDisposable {
	public void Dispose() {
		action();
	}
}