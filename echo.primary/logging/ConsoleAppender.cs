using System.Text;

namespace echo.primary.logging;

public class ConsoleAppender(string name, Level level = Level.Trace, IRenderer? renderer = null) : IAppender {
	public Level Level { get; } = level;
	public string Name { get; set; } = name;
	public IRenderer Renderer { get; } = renderer ?? new SimpleLineRenderer();

	private readonly StringBuilder _sb = new();

	public void Append(LogItem log) {
		lock (_sb) {
			Renderer.Render(_sb, log);
			Console.Write(_sb.ToString());
			_sb.Clear();
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
	Level level = Level.Trace,
	Dictionary<Level, ColorSchema>? schemas = null
) : ConsoleAppender(
	name,
	level,
	new ColorfulSimpleLineRenderer(schemas ?? new())
);