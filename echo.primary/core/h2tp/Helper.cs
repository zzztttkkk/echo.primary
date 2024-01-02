using System.Web;

namespace echo.primary.core.h2tp;

public static class Helper {
	public static string UrlDecode(ReadOnlySpan<char> src) {
		for (var i = 0; i < src.Length; i++) {
			var c = src[i];
			if (c == '%') {
				if (i + 2 < src.Length) {
				}
			}
		}

		return src.ToString();
	}
}