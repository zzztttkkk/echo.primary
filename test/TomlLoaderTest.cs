using System.Drawing;
using echo.primary.core.h2tp;
using echo.primary.utils;

namespace test;

class Point {
	public int X { get; set; }
	public int Y { get; set; }
	public string Name { get; set; }
}

class Foo {
	[Toml(ParserType = typeof(TomlParsers.ColorParser))]
	public Color Color { get; set; }

	[Toml(ParserType = typeof(TomlParsers.ByteSizeParser))]
	public int SizeA { get; set; }

	[Toml(ParserType = typeof(TomlParsers.DurationParser))]
	public TimeSpan MaxAliveDuration { get; set; }

	public List<Point> Points { get; set; }
}

public class TomlLoaderTest {
	[Test]
	public void TestBind() {
		var obj = TomlLoader.Load<Foo>($"{ThisProject.TestPath}/a.toml");
		Console.WriteLine(Json.Stringify(obj));
	}

	[Test]
	public void TestLoad() {
		var opts = TomlLoader.Load<ServerOptions>($"{ThisProject.TestPath}/c.toml");
		Console.WriteLine(Json.Stringify(opts));
	}

	[Test]
	public void TestMerge() {
		var result = TomlLoader.Merge(
			[
				TomlLoader.ParseFile($"{ThisProject.TestPath}/a.toml"),
				TomlLoader.ParseFile($"{ThisProject.TestPath}/b.toml")
			]
		);

		foreach (var pair in result) {
			Console.WriteLine($"{pair.Key}: {pair.Value}");
		}
	}


	[Test]
	public void TestDefer() {
		using (new Defer(() => Console.WriteLine("1"))) {
			using (new Defer(() => Console.WriteLine("2"))) {
				Console.WriteLine("3");
			}
		}
	}
}