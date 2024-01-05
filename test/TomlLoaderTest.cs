using System.Drawing;
using System.Text;
using echo.primary.core.h2tp;
using echo.primary.utils;

namespace test;

class Point {
	public int X { get; set; } = 0;
	public int Y { get; set; } = 0;
	public string Name { get; set; } = "";
}

class Foo {
	[Toml(ParserType = typeof(TomlParsers.ColorParser))]
	public Color Color { get; set; } = Color.Black;

	[Toml(ParserType = typeof(TomlParsers.ByteSizeParser))]
	public int SizeA { get; set; } = 0;

	[Toml(ParserType = typeof(TomlParsers.DurationParser))]
	public TimeSpan MaxAliveDuration { get; set; } = new(0);

	public List<Point> Points { get; set; } = null!;
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

	private static readonly byte[] HexTable = new byte[512];

	public static readonly InitFunc _ = new(() => {
		var digits = "0123456789ABCDEF"u8.ToArray();
		var i = 0;
		foreach (var y in digits) {
			foreach (var x in digits) {
				HexTable[i] = y;
				i++;
				HexTable[i] = x;
				i++;
			}
		}
	});

	[Test]
	public void TestDefer() {
		var tmp = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
		var x = (uint)156;
		var i = 3;
		while (i >= 0) {
			var pos = (x & 0xff) * 2;
			var c = HexTable[pos];
			tmp[i * 2] = c;

			c = HexTable[pos + 1];
			tmp[(i << 1) + 1] = c;

			x >>= 8;
			i -= 1;
		}

		i = 0;
		for (; i < 8; i++) {
			if (tmp[i] != (byte)'0') {
				break;
			}
		}

		if (i >= 7) {
			Console.WriteLine("0");
		}

		Console.WriteLine($"{Encoding.ASCII.GetString(tmp[i..])}");
	}
}