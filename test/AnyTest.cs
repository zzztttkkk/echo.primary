using echo.primary.utils;

namespace test;

public class AnyTest {
	[Test]
	public void HexToString() {
		var random = new Random();
		var c = 100_000;
		while (c > 0) {
			var num = (uint)random.Next();
			if (Hex.ToString(num) != num.ToString("X")) {
				throw new Exception($"{num}: {Hex.ToBytes(num)} {num:X}");
			}

			c--;
		}

		Console.WriteLine("OK");
	}

	[Test]
	public void Any() {
		var now = DateTime.Now;
		Console.WriteLine(now.ToString("R"));
		Console.WriteLine(DateTime.ParseExact(now.ToString("R"), format: "R", provider: null).ToString("R"));
	}
}