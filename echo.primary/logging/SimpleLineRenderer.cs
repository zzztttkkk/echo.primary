using System.Drawing;
using System.Text;
using echo.primary.utils;

namespace echo.primary.logging;

public class SimpleLineRenderer : IRenderer {
	public string TimeLayout { get; set; } = "yyyy-MM-dd HH:mm:ss.fff/z";

	public void Render(StringBuilder builder, LogItem log) {
		// loggerName
		builder.Append($"[{log.LoggerName}] ");

		// time
		builder.Append($"[{log.Time.ToString(TimeLayout)}] ");

		// level
		builder.Append($"[{log.Level}] ");

		// message
		builder.Append(log.Message);

		builder.Append('\n');
	}
}

public class ColorSchema(Color? name = null, Color? time = null, Color? level = null, Color? message = null) {
	[Toml(Optional = true)] public Color? Name { get; set; } = name;
	[Toml(Optional = true)] public Color? Time { get; set; } = time;
	[Toml(Optional = true)] public Color? Level { get; set; } = level;
	[Toml(Optional = true)] public Color? Message { get; set; } = message;
}

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

	public void Render(StringBuilder builder, LogItem log) {
		ColorSchemas.TryGetValue(log.Level, out var schema);

		// loggerName
		WithColor(builder, $"[{log.LoggerName}] ", schema?.Name);

		// time
		WithColor(builder, $"[{log.Time.ToString(TimeLayout)}] ", schema?.Time);

		// level
		WithColor(builder, $"[{log.Level}] ", schema?.Level);

		// message
		WithColor(builder, log.Message, schema?.Message);

		builder.Append('\n');
	}
}