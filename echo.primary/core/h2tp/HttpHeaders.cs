namespace echo.primary.core.h2tp;

public class HttpHeaders {
	private Dictionary<string, List<string>>? data;

	public void Clear() {
		data?.Clear();
	}

	public int KeySize => data?.Count ?? 0;
	public bool Empty => KeySize < 1;

	public List<string>? GetAll(string key, bool isLowercase = false) {
		if (data == null) return null;
		data.TryGetValue(isLowercase ? key : key.ToLower(), out var lst);
		return lst;
	}

	public List<string>? GetAll(HttpRfcHeader key) => GetAll(HeaderToString.ToString(key), true);

	public string? GetFirst(string key, bool isLowercase = false) {
		var lst = GetAll(key, isLowercase);
		if (lst == null || lst.Count < 1) return null;
		return lst.First();
	}

	public string? GetFirst(HttpRfcHeader key) => GetFirst(HeaderToString.ToString(key), true);

	public string? GetLast(string key, bool isLowercase = false) {
		var lst = GetAll(key, isLowercase);
		if (lst == null || lst.Count < 1) return null;
		return lst.Last();
	}

	public string? GetLast(HttpRfcHeader key) => GetLast(HeaderToString.ToString(key), true);

	public void Add(string key, string val, bool isLowercase = false) {
		var lst = GetAll(key, isLowercase);
		if (lst != null) {
			lst.Add(val);
			return;
		}

		data ??= new();
		data[isLowercase ? key : key.ToLower()] = [val];
	}

	public void Add(HttpRfcHeader key, string val) => Add(HeaderToString.ToString(key), val, true);

	public void Set(string key, string val, bool isLowercase = false) {
		data ??= new();
		data[isLowercase ? key : key.ToLower()] = [val];
	}

	public void Set(HttpRfcHeader key, string val) => Set(HeaderToString.ToString(key), val, true);

	public void Del(string key, bool isLowercase = false) {
		data?.Remove(isLowercase ? key : key.ToLower());
	}

	public void Del(HttpRfcHeader key) => Del(HeaderToString.ToString(key), true);

	public delegate void Visitor(string key, List<string> lst);

	public void Each(Visitor visitor) {
		if (data == null) return;
		foreach (var pair in data) {
			visitor(pair.Key, pair.Value);
		}
	}

	public long? ContentLength {
		get {
			var txt = GetLast(HttpRfcHeader.ContentLength);
			if (string.IsNullOrEmpty(txt)) return null;
			if (long.TryParse(txt, out var num)) {
				return num;
			}

			return null;
		}
		set {
			if (value is null or < 0) {
				Del(HttpRfcHeader.ContentLength);
				return;
			}

			Set(HttpRfcHeader.ContentLength, value.ToString()!);
		}
	}

	public CompressType? AcceptedCompressType {
		get {
			var lst = GetAll(HttpRfcHeader.AcceptEncoding);
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
		get => GetLast(HttpRfcHeader.ContentType);
		set {
			if (string.IsNullOrEmpty(value)) {
				Del(HttpRfcHeader.ContentType);
				return;
			}

			Set(HttpRfcHeader.ContentType, value);
		}
	}
}

public enum CompressType {
	Brotil,
	Deflate,
	GZip
}