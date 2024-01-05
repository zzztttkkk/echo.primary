using System.Runtime.InteropServices;
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

	private static void PrintAddress(object obj) {
		var handle = GCHandle.Alloc(obj, GCHandleType.Pinned);
		var ptr = GCHandle.ToIntPtr(handle);
		Console.WriteLine($"0x{ptr:X}");
	}

	[Test]
	public void Any() {
		var a = "xxxx";
		var b = a;

		PrintAddress(a);
		PrintAddress(b);
	}
}