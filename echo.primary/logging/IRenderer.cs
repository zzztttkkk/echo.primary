namespace echo.primary.logging;

public interface IRenderer {
	string Render(LogItem log);
}