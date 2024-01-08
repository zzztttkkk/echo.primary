using System.Text;

namespace echo.primary.utils;

public class Authority {
	public string? Username { get; set; }
	public string? Password { get; set; }
	public string? Host { get; set; }
	public ushort? Port { get; set; }
}

public class Uri(string raw) {
	private static readonly bool[] SafeUrlCharTable = new bool[256];

	// ReSharper disable once UnusedMember.Local
	private static readonly InitFunc _ = new(() => {
		Array.Fill(SafeUrlCharTable, false);

		foreach (var (begin, end) in new[] { ('a', 'z'), ('A', 'Z'), ('0', '9') }) {
			for (var c = begin; c <= end; c++) {
				SafeUrlCharTable[c] = true;
			}
		}

		foreach (var c in "- _ . ! ~ * ' ( ) ; / ? : @ & = + $ , # + % [ ]".Where(c => c != ' ')) {
			SafeUrlCharTable[c] = true;
		}
	});

	public string? Scheme { get; set; }
	public Authority? Authority { get; set; }
	public string? Path { get; set; }
	public string? QueryString { get; set; }
	public string? Fragment { get; set; }

	public string? Username {
		get => Authority?.Username;
		set {
			if (value == null && Authority == null) return;
			Authority ??= new();
			Authority.Username = value;
		}
	}

	public string? Password {
		get => Authority?.Password;
		set {
			if (value == null && Authority == null) return;
			Authority ??= new();
			Authority.Password = value;
		}
	}

	public string? Host {
		get => Authority?.Host;
		set {
			if (value == null && Authority == null) return;
			Authority ??= new();
			Authority.Host = value;
		}
	}

	public ushort? Port {
		get => Authority?.Port;
		set {
			if (value == null && Authority == null) return;
			Authority ??= new();
			Authority.Port = value;
		}
	}

	public string Encode() {
		if (string.IsNullOrEmpty(raw)) return "";
		var sb = new StringBuilder();
		if (!string.IsNullOrEmpty(Scheme)) {
			sb.Append($"{Scheme}://");
		}

		if (Username != null) {
			sb.Append(Username);
		}

		if (Password != null) {
			if (Username != null) {
				sb.Append(':');
			}

			sb.Append(Password);
			sb.Append('@');
		}

		if (Host != null) {
			sb.Append(Host);
		}

		if (Port.HasValue) {
			sb.Append(':');
			sb.Append(Port);
		}

		if (Path != null) {
			sb.Append(Path);
		}

		if (QueryString != null) {
			sb.Append('?');
			sb.Append(QueryString);
		}

		if (Fragment == null) return sb.ToString();

		sb.Append('#');
		sb.Append(Fragment);
		return sb.ToString();
	}

	public override string ToString() {
		var sb = new StringBuilder();
		sb.Append($"{nameof(Uri)}[");

		if (!string.IsNullOrEmpty(Scheme)) {
			sb.Append($"<{nameof(Scheme)}: {Scheme}>");
		}

		if (!string.IsNullOrEmpty(Authority?.Username)) {
			sb.Append($"<Username: {Authority?.Username}>");
		}

		if (!string.IsNullOrEmpty(Authority?.Password)) {
			sb.Append($"<Password: {Authority?.Password}>");
		}

		if (!string.IsNullOrEmpty(Authority?.Host)) {
			sb.Append($"<Host: {Authority?.Host}>");
		}

		if (Authority is { Port: not null }) {
			sb.Append($"<Port: {Authority.Port}>");
		}

		if (!string.IsNullOrEmpty(Path)) {
			sb.Append($"<{nameof(Path)}: {Path}>");
		}

		if (!string.IsNullOrEmpty(QueryString)) {
			sb.Append($"<{nameof(QueryString)}: {QueryString}>");
		}

		if (!string.IsNullOrEmpty(Fragment)) {
			sb.Append($"<{nameof(Fragment)}: {Fragment}>");
		}

		sb.Append(']');
		return sb.ToString();
	}

	private string? ParseRaw(bool allowAuthority) {
		if (string.IsNullOrEmpty(raw)) return null;
		var remain = raw;

		if (raw.Any(c => c >= 255 || !SafeUrlCharTable[c])) {
			return "bad char in raw";
		}


		var idx = remain.LastIndexOf('#');
		if (idx > -1) {
			Fragment = Unescape(remain[(idx + 1)..]);
			remain = remain[..idx];
		}

		if (string.IsNullOrEmpty(remain)) return null;

		idx = remain.IndexOf('?');
		if (idx > -1) {
			QueryString = remain[(idx + 1)..];
			remain = remain[..idx];
		}

		if (string.IsNullOrEmpty(remain)) return null;

		if (!allowAuthority) {
			return SchemeAndPath();
		}

		idx = remain.IndexOf("//", StringComparison.Ordinal);
		if (idx > -1) {
			var sidx = remain.IndexOf(':', 0, idx);
			if (sidx > -1) {
				Scheme = remain[..sidx].ToLower();
			}

			remain = remain[(idx + 2)..];

			Authority ??= new Authority();

			idx = remain.IndexOf('@');
			if (idx > -1) {
				var up = remain[..idx];
				remain = remain[(idx + 1)..];
				idx = up.IndexOf(':');
				if (idx > -1) {
					Authority.Username = Unescape(up[..idx]);
					Authority.Password = Unescape(up[(idx + 1)..]);
				}
				else {
					Authority.Username = Unescape(up);
				}
			}

			if (string.IsNullOrEmpty(remain)) return null;

			if (remain.StartsWith('[')) {
				idx = remain.IndexOf(']');
				if (idx < 0) return "bad host, maybe a ipv6";
				Authority.Host = Unescape(remain[1..idx]);
				remain = remain[(idx + 1)..];
			}

			idx = remain.IndexOf(':');
			if (idx > -1) {
				if (Authority.Host == null) {
					Authority.Host = Unescape(remain[..idx]);
					remain = remain[(idx + 1)..];
				}
				else if (idx != 0) {
					return "bad host or port for ipv6";
				}

				var tmp = "";
				foreach (var c in remain) {
					if (c is >= '0' and <= '9') {
						tmp += c;
						continue;
					}

					break;
				}

				if (string.IsNullOrEmpty(tmp)) return "empty port";

				if (!ushort.TryParse(tmp, out var pn)) {
					return $"bad port part: ${tmp}";
				}

				Authority.Port = pn;
				remain = remain[tmp.Length..];
			}

			Path = Unescape(remain);
		}
		else {
			return SchemeAndPath();
		}

		if (Authority is not { Port: null }) return null;

		Authority.Port = Scheme switch {
			"http" => 80,
			"https" => 443,
			"mysql" => 3306,
			"redis" => 6379,
			"postgres" => 5432,
			_ => Authority.Port
		};
		return null;

		string? SchemeAndPath() {
			idx = remain.IndexOf(':');
			if (idx > -1) {
				Scheme = remain[..idx];
				remain = remain[(idx + 1)..];
			}

			Path = Unescape(remain);
			return null;
		}
	}

	public static (Uri?, string?) Parse(string raw, bool allowAuthority = true) {
		var obj = new Uri(raw);
		var exc = obj.ParseRaw(allowAuthority);
		if (exc != null) return (null, exc);
		return (obj, null);
	}

	public static string Unescape(string v) {
		if (!v.Contains('%')) return v;

		var sb = new StringBuilder(v.Length);
		var view = v.AsSpan();

		for (var i = 0; i < view.Length; i++) {
			var c = view[i];
			switch (c) {
				case '%': {
					sb.Append((byte)((Hex.HexIntTable[view[i + 1]] << 4) | Hex.HexIntTable[view[i + 2]]));
					i += 2;
					break;
				}
				case '+': {
					sb.Append((byte)' ');
					break;
				}
				default: {
					sb.Append(c);
					break;
				}
			}
		}

		return sb.ToString();
	}
}