using System.Text.Json;
using System.Text.Json.Serialization;
using echo.primary.utils;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace test;

class Obj {
	[Ini] public ConsoleColor Color { get; set; }

	[Ini(ParserType = typeof(Parsers.ByteSizeParser))] public ulong SizeA { get; set; }
}

internal class EnumConverter : JsonConverter<ConsoleColor> {
	public override ConsoleColor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
		throw new NotImplementedException();
	}

	public override void Write(Utf8JsonWriter writer, ConsoleColor value, JsonSerializerOptions options) {
		writer.WriteStringValue($"{value.ToString()}");
	}
}

public class IniTest {
	[Test]
	public void TestLoad() {
		var obj = IniLoader.Parse<Obj>($"{EchoPrimaryProject.ProjectRoot()}/test/v.ini");
		var opt = new JsonSerializerOptions();
		opt.Converters.Add(new EnumConverter());
		Console.WriteLine(JsonSerializer.Serialize(obj, options: opt));
	}
}