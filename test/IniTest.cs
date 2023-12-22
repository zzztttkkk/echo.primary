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
		Console.WriteLine(string.Format("{0}", 1221));
	}
}