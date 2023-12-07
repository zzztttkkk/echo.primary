using echo.primary.core.net;

namespace test;

public class Tests {
	[SetUp]
	public void Setup() {
	}


	[Test]
	public void Test1() {
		var a = new byte[10];
		for (var i = 0; i < a.Length; i++) {
			a[i] = 97;
		}

		var mv = new Memory<byte>(a, 0, 10);
		Console.WriteLine($"{mv}");
	}
}