using echo.primary.utils;

namespace echo.primary.core.h2tp;

public class Headers() : MultiMap(true) {
	public List<string>? GetAll(RfcHeader key) => GetAll(HeaderToString.ToString(key), true);
	public string? GetFirst(RfcHeader key) => GetFirst(HeaderToString.ToString(key), true);
	public string? GetLast(RfcHeader key) => GetLast(HeaderToString.ToString(key), true);
	public void Add(RfcHeader key, string val) => Add(HeaderToString.ToString(key), val, true);
	public void Set(RfcHeader key, string val) => Set(HeaderToString.ToString(key), val, true);
	public void Del(RfcHeader key) => Del(HeaderToString.ToString(key), true);

	public long? ContentLength {
		get {
			var txt = GetLast(RfcHeader.ContentLength);
			if (string.IsNullOrEmpty(txt)) return null;
			if (long.TryParse(txt, out var num)) {
				return num;
			}

			return null;
		}
		set {
			if (value is null or < 0) {
				Del(RfcHeader.ContentLength);
				return;
			}

			Set(RfcHeader.ContentLength, value.ToString()!);
		}
	}

	public CompressType? AcceptedCompressType {
		get {
			var lst = GetAll(RfcHeader.AcceptEncoding);
			if (lst == null || lst.Count < 1) {
				return null;
			}

			foreach (
				var name in lst.SelectMany(
					hv => hv
						.Split(",")
						.Select(v => v.Split(";")[0].Trim())
						.Where(v => !string.IsNullOrEmpty(v))
						.Select(v => v.ToLower())
				)
			) {
				switch (name) {
					case "gzip": {
						return CompressType.GZip;
					}
					case "deflate": {
						return CompressType.Deflate;
					}
					case "br": {
						return CompressType.Brotil;
					}
					case "*": {
						return CompressType.GZip;
					}
				}
			}

			return null;
		}
	}

	public string? ContentType {
		get => GetLast(RfcHeader.ContentType);
		set {
			if (string.IsNullOrEmpty(value)) {
				Del(RfcHeader.ContentType);
				return;
			}

			Set(RfcHeader.ContentType, value);
		}
	}
}

public enum CompressType {
	Brotil,
	Deflate,
	GZip
}