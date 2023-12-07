using echo.primary.core.tcp;
using echo.primary.logging;

namespace test;

public class Tests {
	[SetUp]
	public void Setup() {
	}


	[Test]
	public void Test1() {
		var buf = new BytesBuffer();
		buf.Write("hello world");

		Console.WriteLine($"{buf.Size} {buf.Capacity} {buf.Offset}");
	}
}