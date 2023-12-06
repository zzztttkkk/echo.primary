namespace echo.primary.logging;

public interface IRenderer {
	string Render(string name, LogItem log);
}