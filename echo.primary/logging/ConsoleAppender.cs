namespace echo.primary.logging;

public class ConsoleAppender : IAppender {
	public ConsoleAppender(string name, Level level, IRenderer? renderer = null) {
		Level = level;
		Name = name;
		Renderer = renderer ?? new SimpleLineRenderer();
	}

	public Level Level { get; }
	public string Name { get; }
	public IRenderer Renderer { get; }

	public void Append(LogItem log) {
		Console.Write(Renderer.Render(log));
	}

	public void Flush() {
		Console.Out.Flush();
	}
}