namespace echo.primary.logging;

public class ConsoleAppender(string name, Level level = Level.TRACE, IRenderer? renderer = null) : IAppender {
	public Level Level { get; } = level;
	public string Name { get; } = name;
	public IRenderer Renderer { get; } = renderer ?? new SimpleLineRenderer();

	public void Append(LogItem log) {
		Console.Write(Renderer.Render(Name, log));
	}

	public void Flush() {
		Console.Out.Flush();
	}

	public void Close() {
		Flush();
	}
}