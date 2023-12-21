using System.Drawing;
using System.Text;
using echo.primary.utils;

namespace test;

class Obj {
	public Color Color { get; set; }

	[Ini(ParserType = typeof(Parsers.ByteSizeParser))] public int SizeA { get; set; }

	public TimeSpan MaxAliveDuration { get; set; }
}

public class IniTest {
	[Test]
	public void TestLoad() {
		var obj = IniLoader.Parse<Obj>($"{EchoPrimaryProject.ProjectRoot()}/test/v.ini");
		Console.WriteLine(JSON.Stringify(obj));
	}

	[Test]
	public void TestBuffer() {
		var ms = new MemoryStream(10);
		var ws = new BinaryWriter(ms, encoding: Encoding.UTF8, leaveOpen: true);
		var rs = new BinaryReader(ms, encoding: Encoding.UTF8, leaveOpen: true);
		ws.Write("123456789");
		Console.WriteLine($"{ms.Position} {ms.Length}");
		ms.Position = 0;
		Console.WriteLine($"{ms.Position} {ms.Length}");
	}
}