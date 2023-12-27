using System.Drawing;
using echo.primary.utils;

namespace test;

class Foo {
	public Color Color { get; set; }

	public int SizeA { get; set; }

	public TimeSpan MaxAliveDuration { get; set; }
}

public class TomlLoaderTest {
	[Test]
	public void TestBind() {
		var obj = TomlLoader.Load<Foo>($"{EchoPrimaryProject.ProjectRoot()}/test/v.toml");
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