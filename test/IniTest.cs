using System.Drawing;
using System.Text.Json;
using echo.primary.utils;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace test;

class Obj {
	public Color Color { get; set; }

	[Ini(ParserType = typeof(Parsers.ByteSizeParser))]
	public int SizeA { get; set; }
}

public class IniTest {
	[Test]
	public void TestLoad() {
		var obj = IniLoader.Parse<Obj>($"{EchoPrimaryProject.ProjectRoot()}/test/v.ini");
		Console.WriteLine(JSON.Stringify(obj));
	}
}