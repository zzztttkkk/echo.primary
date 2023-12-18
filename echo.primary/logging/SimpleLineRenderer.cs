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

public record ColorOptions(
	ConsoleColor? Name = null,
	ConsoleColor? Time = null,
	ConsoleColor? Level = null,
	ConsoleColor? Action = null,
	ConsoleColor? Message = null,
	ConsoleColor? ArgsTitle = null,
	ConsoleColor? ArgsValue = null,
	ConsoleColor? ArgsSep = null
);

internal class ColorfulSimpleLineRenderer(ColorOptions options) : IRenderer {
	public string TimeLayout { get; set; } = "yyyy-MM-dd HH:mm:ss.fff/z";
	private ColorOptions ColorOptions { get; } = options;

	private void WithColor(StringBuilder builder, string val, ConsoleColor? color) {
		if (color == null) {
			builder.Append(val);
			return;
		}

		var matched = true;

		switch (color) {
			case ConsoleColor.Black:
				builder.Append("\x1b[38;5;0m");
				break;
			case ConsoleColor.DarkBlue:
				builder.Append("\x1b[38;5;4m");
				break;
			case ConsoleColor.DarkGreen:
				builder.Append("\x1b[38;5;2m");
				break;
			case ConsoleColor.DarkCyan:
				builder.Append("\x1b[38;5;6m");
				break;
			case ConsoleColor.DarkRed:
				builder.Append("\x1b[38;5;1m");
				break;
			case ConsoleColor.DarkMagenta:
				builder.Append("\x1b[38;5;5m");
				break;
			case ConsoleColor.DarkYellow:
				builder.Append("\x1b[38;5;3m");
				break;
			case ConsoleColor.Gray:
				builder.Append("\x1b[38;5;8m");
				break;
			case ConsoleColor.DarkGray:
				builder.Append("\x1b[38;5;7m");
				break;
			case ConsoleColor.Blue:
				builder.Append("\x1b[38;5;12m");
				break;
			case ConsoleColor.Green:
				builder.Append("\x1b[38;5;40m");
				break;
			case ConsoleColor.Cyan:
				builder.Append("\x1b[38;5;14m");
				break;
			case ConsoleColor.Red:
				builder.Append("\x1b[38;5;9m");
				break;
			case ConsoleColor.Magenta:
				builder.Append("\x1b[38;5;13m");
				break;
			case ConsoleColor.Yellow:
				builder.Append("\x1b[38;5;11m");
				break;
			case ConsoleColor.White:
				builder.Append("\x1b[38;5;15m");
				break;
			default:
				matched = false;
				break;
		}

		builder.Append(val);
		if (matched) builder.Append("\x1b[0m");
	}

	public void Render(StringBuilder builder, string name, LogItem log) {
		// loggerName
		if (!string.IsNullOrEmpty(name)) {
			WithColor(builder, $"[{name}] ", ColorOptions.Name);
		}

		// time
		WithColor(builder, $"[{log.time.ToString(TimeLayout)}] ", ColorOptions.Time);

		// level
		WithColor(builder, $"[{log.level}] ", ColorOptions.Level);

		// action
		if (!string.IsNullOrEmpty(log.action)) {
			WithColor(builder, $"[{log.action}.", ColorOptions.Action);
		}

		// message
		WithColor(builder, log.msg, ColorOptions.Message);

		if (log.args is { Count: > 0 }) {
			WithColor(builder, " [Args: ", ColorOptions.ArgsTitle);
			foreach (var obj in log.args) {
				WithColor(
					builder,
					obj.ToString() ?? $"{obj.GetType().Name}<{obj.GetHashCode()}>",
					ColorOptions.ArgsValue
				);
				WithColor(builder, ",", ColorOptions.ArgsSep);
			}

			WithColor(builder, "] ", ColorOptions.ArgsTitle);
		}

		builder.Append('\n');
	}
}