using System.Drawing;
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

public record ColorSchema(
	Color? Name = null,
	Color? Time = null,
	Color? Level = null,
	Color? Action = null,
	Color? Message = null,
	Color? ArgsTitle = null,
	Color? ArgsValue = null,
	Color? ArgsSep = null
);

internal class ColorfulSimpleLineRenderer(Dictionary<Level, ColorSchema> schemas) : IRenderer {
	public string TimeLayout { get; set; } = "yyyy-MM-dd HH:mm:ss.fff/z";
	private Dictionary<Level, ColorSchema> ColorSchemas { get; } = schemas;

	private static void WithColor(StringBuilder builder, string val, Color? color) {
		if (color == null) {
			builder.Append(val);
			return;
		}

		builder.Append($"\x1b[38;2;{color.Value.R};{color.Value.G};{color.Value.B}m");
		builder.Append(val);
		builder.Append("\x1b[0m");
	}

	public void Render(StringBuilder builder, string name, LogItem log) {
		ColorSchemas.TryGetValue(log.level, out var schema);
		schema ??= new ColorSchema();

		// loggerName
		if (!string.IsNullOrEmpty(name)) {
			WithColor(builder, $"[{name}] ", schema.Name);
		}

		// time
		WithColor(builder, $"[{log.time.ToString(TimeLayout)}] ", schema.Time);

		// level
		WithColor(builder, $"[{log.level}] ", schema.Level);

		// action
		if (!string.IsNullOrEmpty(log.action)) {
			WithColor(builder, $"[{log.action}.", schema.Action);
		}

		// message
		WithColor(builder, log.msg, schema.Message);

		if (log.args is { Count: > 0 }) {
			WithColor(builder, " [Args: ", schema.ArgsTitle);
			foreach (var obj in log.args) {
				WithColor(
					builder,
					obj.ToString() ?? $"{obj.GetType().Name}<{obj.GetHashCode()}>",
					schema.ArgsValue
				);
				WithColor(builder, ",", schema.ArgsSep);
			}

			WithColor(builder, "] ", schema.ArgsTitle);
		}

		builder.Append('\n');
	}
}