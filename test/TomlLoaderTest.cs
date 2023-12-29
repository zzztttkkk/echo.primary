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
	[Toml(ParserType = typeof(TomlParsers.ColorParser))] public Color Color { get; set; }

	[Toml(ParserType = typeof(TomlParsers.ByteSizeParser))] public int SizeA { get; set; }

	[Toml(ParserType = typeof(TomlParsers.DurationParser))] public TimeSpan MaxAliveDuration { get; set; }

	public List<Point> Points { get; set; }
}

public class TomlLoaderTest {
	[Test]
	public void TestBind() {
		var obj = TomlLoader.Load<Foo>($"{EchoPrimaryProject.ProjectRoot()}/test/a.toml");
		Console.WriteLine(JSON.Stringify(obj));
	}

	[Test]
	public void TestLoad() {
		var opts = TomlLoader.Load<ServerOptions>($"{EchoPrimaryProject.ProjectRoot()}/test/c.toml");
		Console.WriteLine(JSON.Stringify(opts));
	}

	[Test]
	public void TestMerge() {
		var result = TomlLoader.Merge(
			[
				TomlLoader.ParseFile($"{EchoPrimaryProject.ProjectRoot()}/test/a.toml"),
				TomlLoader.ParseFile($"{EchoPrimaryProject.ProjectRoot()}/test/b.toml")
			]
		);

		foreach (var pair in result) {
			Console.WriteLine($"{pair.Key}: {pair.Value}");
		}
	}


	[Test]
	public void TestDefered() {
		using (new Defered(() => Console.WriteLine("1"))) {
			using (new Defered(() => Console.WriteLine("2"))) {
				Console.WriteLine("3");
			}
		}
	}
}