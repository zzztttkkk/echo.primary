using System.Drawing;
using echo.primary.utils;

namespace test;

class Foo {
	[Toml(ParserType = typeof(TomlParsers.ColorParser))] public Color Color { get; set; }

	[Toml(ParserType = typeof(TomlParsers.ByteSizeParser))] public int SizeA { get; set; }

	[Toml(ParserType = typeof(TomlParsers.TimeDurationParser))] public TimeSpan MaxAliveDuration { get; set; }
}

public class TomlLoaderTest {
	[Test]
	public void TestBind() {
		var obj = TomlLoader.Load<Foo>($"{EchoPrimaryProject.ProjectRoot()}/test/a.toml");
		Console.WriteLine(JSON.Stringify(obj));
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
}