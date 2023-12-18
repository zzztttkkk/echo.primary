using System.Text;

namespace echo.primary.logging;

public class ConsoleAppender(string name, Level level = Level.TRACE, IRenderer? renderer = null) : IAppender {
	public Level Level { get; } = level;
	public string Name { get; set; } = name;
	public IRenderer Renderer { get; } = renderer ?? new SimpleLineRenderer();

	private readonly StringBuilder sb = new();

	public void Append(LogItem log) {
		lock (sb) {
			Renderer.Render(sb, Name, log);
			Console.Write(sb.ToString());
			sb.Clear();
		}
	}

	public void Flush() {
		Console.Out.Flush();
	}

	public void Close() {
		Flush();
	}
}

public class ColorfulConsoleAppender(
	string name,
	Level level = Level.TRACE,
	ColorOptions? colors = null
) : ConsoleAppender(
	name,
	level,
	new ColorfulSimpleLineRenderer(colors ?? new ColorOptions())
);