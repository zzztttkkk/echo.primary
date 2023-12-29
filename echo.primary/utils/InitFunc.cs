namespace echo.primary.utils;

public class InitFunc {
	public InitFunc(Action action) {
		action();
	}
}