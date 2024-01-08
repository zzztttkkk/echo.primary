using System.Text;

namespace echo.primary.utils;

public static class Hex {
	private static readonly byte[] HexTable = new byte[512];
	internal static readonly byte[] HexIntTable = new byte[256];

	private static readonly InitFunc _ = new(() => {
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

		foreach (var c in "0123456789") {
			HexIntTable[c] = (byte)(c - '0');
		}

		foreach (var c in "abcdef") {
			HexIntTable[c] = (byte)(c - 'a');
		}

		foreach (var c in "ABCDEF") {
			HexIntTable[c] = (byte)(c - 'A');
		}
	});

	public static string ToString(uint x) {
		return Encoding.ASCII.GetString(ToBytes(x));
	}


	// https://johnnylee-sde.github.io/Fast-unsigned-integer-to-hex-string/
	public static byte[] ToBytes(uint x) {
		var tmp = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
		var i = 3;
		while (i >= 0) {
			var pos = (x & 0xff) * 2;
			tmp[i << 1] = HexTable[pos];
			tmp[(i << 1) + 1] = HexTable[pos + 1];

			x >>= 8;
			i -= 1;
		}

		i = 0;
		for (; i < 8; i++) {
			if (tmp[i] != (byte)'0') {
				break;
			}
		}

		return i >= 7 ? "0"u8.ToArray() : tmp[i..];
	}
}