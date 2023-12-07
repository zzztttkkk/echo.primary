namespace echo.primary.logging;

public interface IRenderer {
	string TimeLayout { get; set; }

	string Render(string name, LogItem log);
}