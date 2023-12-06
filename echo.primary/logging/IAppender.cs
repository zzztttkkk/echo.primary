namespace echo.primary.logging;

public interface IAppender {
	Level Level { get; }
	string Name { get; }

	IRenderer Renderer { get; }

	void Append(LogItem log);
	void Flush();
}