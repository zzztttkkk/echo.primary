using System.Text;

namespace echo.primary.logging;

public record ColorOption(ConsoleColor? Frontground = null, ConsoleColor? Background = null);

public record ColorOptions(
	ColorOption Time,
	ColorOption Level,
	ColorOption Action,
	ColorOption Path,
	ColorOption Message,
	ColorOption Args
);

public class SimpleLineRenderer : IRenderer {
	public string Render(string name, LogItem log) {
		StringBuilder builder = new();

		// loggerName
		builder.Append($"[{name}] ");

		// time
		builder.Append($"[{log.time}] ");

		// level
		builder.Append($"[{log.level}] ");

		// action.path
		if (log.action.Length > 0) {
			builder.Append($"[{log.action}.");
		}

		if (log.path.Length > 0) {
			builder.Append($"{log.path}] ");
		}

		// message
		builder.Append(log.msg);

		if (log.args != null) {
			builder.Append(" [Args: ");
			foreach (var obj in log.args) {
				builder.Append(obj);
				builder.Append(',');
			}

			builder.Append(" ]");
		}

		builder.Append('\n');
		return builder.ToString();
	}
}