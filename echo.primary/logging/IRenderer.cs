namespace echo.primary.logging;

public interface IRenderer {
	string Render(string loggername, LogItem log);
}