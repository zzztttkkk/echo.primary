using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace echo.primary.utils;

class ColorConverter : JsonConverter<Color> {
	public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
		throw new NotImplementedException();
	}

	public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options) {
		writer.WriteStringValue(value.Name);
	}
}

public static class JSON {
	private static readonly List<JsonConverter> Converters = [new ColorConverter()];

	public static void AddCustomConverter(JsonConverter converter) {
		Converters.Add(converter);
	}

	private static JsonSerializerOptions opts {
		get {
			var _opts = new JsonSerializerOptions { WriteIndented = true };
			foreach (var converter in Converters) {
				_opts.Converters.Add(converter);
			}

			return _opts;
		}
	}

	public static string Stringify(object val) => JsonSerializer.Serialize(val, opts);

	public static void Stringify(Stream dst, object val) => JsonSerializer.Serialize(dst, val, opts);

	public static T? Parse<T>(string val) => JsonSerializer.Deserialize<T>(val, opts);

	public static T? Parse<T>(Stream val) => JsonSerializer.Deserialize<T>(val, opts);
}