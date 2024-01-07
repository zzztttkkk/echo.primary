﻿using echo.primary.utils;
using Uri = echo.primary.utils.Uri;

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
		Console.WriteLine(Uri.Parse(""));
	}
}