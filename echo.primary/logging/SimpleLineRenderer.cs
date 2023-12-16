using System.Text;

namespace echo.primary.logging;

public class SimpleLineRenderer : IRenderer {
	public string TimeLayout { get; set; } = "yyyy-MM-dd HH:mm:ss.fff/z";

	public void Render(StringBuilder builder, string name, LogItem log) {
		// loggerName
		if (!string.IsNullOrEmpty(name)) {
			builder.Append($"[{name}] ");
		}

		// time
		builder.Append($"[{log.time.ToString(TimeLayout)}] ");

		// level
		builder.Append($"[{log.level}] ");

		// action.path
		if (!string.IsNullOrEmpty(log.action)) {
			builder.Append($"[{log.action}.");
		}

		if (!string.IsNullOrEmpty(log.path)) {
			builder.Append($"{log.path}] ");
		}

		// message
		builder.Append(log.msg);

		if (log.args is { Count: > 0 }) {
			builder.Append(" [Args: ");
			foreach (var obj in log.args) {
				builder.Append(obj);
				builder.Append(',');
			}

			builder.Append(" ]");
		}

		builder.Append('\n');
	}
}

public record ColorOption(ConsoleColor? Frontground = null, ConsoleColor? Background = null);

public record ColorOptions(
	ColorOption? Name = null,
	ColorOption? Time = null,
	ColorOption? Level = null,
	ColorOption? Action = null,
	ColorOption? Path = null,
	ColorOption? Message = null,
	ColorOption? Args = null
);

internal class ColorfulSimpleLineRenderer : IRenderer {
	public string TimeLayout { get; set; } = "yyyy-MM-dd HH:mm:ss.fff/z";
	public ColorOptions ColorOptions { get; set; } = new ColorOptions();

	private void ApplyColor(ColorOption? opt) {
		if (opt == null) return;
		if (opt.Background != null) {
			Console.BackgroundColor = opt.Background.Value;
		}

		if (opt.Frontground != null) {
			Console.ForegroundColor = opt.Frontground.Value;
		}
	}

	public void Render(StringBuilder builder, string name, LogItem log) {
		// loggerName
		if (!string.IsNullOrEmpty(name)) {
			ApplyColor(ColorOptions.Name);
			builder.Append($"[{name}] ");
			Console.ResetColor();
		}

		// time
		builder.Append($"[{log.time.ToString(TimeLayout)}] ");

		// level
		builder.Append($"[{log.level}] ");

		// action.path
		if (!string.IsNullOrEmpty(log.action)) {
			builder.Append($"[{log.action}.");
		}

		if (!string.IsNullOrEmpty(log.path)) {
			builder.Append($"{log.path}] ");
		}

		// message
		builder.Append(log.msg);

		if (log.args is { Count: > 0 }) {
			builder.Append(" [Args: ");
			foreach (var obj in log.args) {
				builder.Append(obj);
				builder.Append(',');
			}

			builder.Append(" ]");
		}

		builder.Append('\n');
	}
}