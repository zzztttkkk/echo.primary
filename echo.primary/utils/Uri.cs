using System.Text;

namespace echo.primary.utils;

public class Authority {
	public string? Name { get; set; }
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

		foreach (var c in "- _ . ! ~ * ' ( ) ; / ? : @ & = + $ , # + %".Where(c => c != ' ')) {
			SafeUrlCharTable[c] = true;
		}
	});

	public string? Scheme { get; set; }
	public Authority? Authority { get; set; }
	public string? Path { get; set; }
	public string? QueryString { get; set; }
	public string? Fragment { get; set; }

	public string? Username {
		get => Authority?.Name;
		set {
			if (value == null && Authority == null) return;
			Authority ??= new();
			Authority.Name = value;
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

	public override string ToString() {
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

	public string Fmt() {
		var sb = new StringBuilder();
		sb.Append($"{nameof(Uri)}[");

		if (!string.IsNullOrEmpty(Scheme)) {
			sb.Append($"<{nameof(Scheme)}: {Scheme}>");
		}

		if (!string.IsNullOrEmpty(Authority?.Name)) {
			sb.Append($"<Username: {Authority?.Name}>");
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

	private Exception? parse(bool AllowAuthority) {
		if (string.IsNullOrEmpty(raw)) return null;

		if (raw.Any(c => c >= 255 || !SafeUrlCharTable[c])) {
			return new Exception("bad char in raw");
		}

		var remain = raw;

		var idx = remain.LastIndexOf('#');
		if (idx > -1) {
			Fragment = remain[(idx + 1)..];
			remain = remain[..idx];
		}

		if (string.IsNullOrEmpty(remain)) return null;

		idx = remain.LastIndexOf('?');
		if (idx > -1) {
			QueryString = remain[(idx + 1)..];
			remain = remain[..idx];
		}

		if (string.IsNullOrEmpty(remain)) return null;

		if (!AllowAuthority) {
			if (remain.StartsWith('/')) {
				Path = remain;
			}
			else {
				Path = '/' + remain;
			}

			return null;
		}

		idx = remain.IndexOf("://", StringComparison.Ordinal);
		if (idx > -1) {
			Scheme = remain[..idx].ToLower();
			remain = remain[(idx + 4)..];
		}

		if (string.IsNullOrEmpty(remain)) return null;

		idx = remain.LastIndexOf('/');
		if (idx > -1) {
			Path = remain[idx..];
			remain = remain[..idx];
		}

		if (string.IsNullOrEmpty(remain)) return null;

		Authority ??= new Authority();

		idx = remain.IndexOf('@');
		if (idx > -1) {
			var up = remain[..idx];
			remain = remain[(idx + 1)..];

			idx = up.IndexOf(':');
			if (idx > -1) {
				Authority.Name = up[..idx];
				Authority.Password = up[(idx + 1)..];
			}
			else {
				Authority.Name = up;
			}
		}

		if (string.IsNullOrEmpty(remain)) return null;

		if (remain.StartsWith('[')) {
			idx = remain.LastIndexOf(']');
			if (idx < 0) return new Exception("bad host, maybe a ipv6");

			Authority.Host = remain[1..idx];
			remain = remain[(idx + 1)..];
			if (string.IsNullOrEmpty(remain)) return null;
			if (ushort.TryParse(remain, out var pn)) {
				Authority.Port = pn;
			}
			else {
				return new Exception($"bad port value, {remain}");
			}

			return null;
		}

		idx = remain.IndexOf(':');
		if (idx > -1) {
			Authority.Host = remain[..idx];
			if (ushort.TryParse(remain[(idx + 1)..], out var pn)) {
				Authority.Port = pn;
			}
			else {
				return new Exception($"bad port value, {remain[(idx + 1)..]}");
			}
		}
		else {
			if (Path == null && !remain.Contains('.')) {
				Path = '/' + remain;
			}
			else {
				Authority.Host = remain;
			}
		}

		if (Authority.Port != null) return null;

		Authority.Port = Scheme switch {
			"http" => 80,
			"https" => 443,
			"mysql" => 3306,
			"redis" => 6379,
			"postgres" => 5432,
			_ => Authority.Port
		};
		return null;
	}

	public static (Uri?, Exception?) Parse(string raw, bool AllowAuthority = false) {
		var obj = new Uri(raw);
		var exc = obj.parse(AllowAuthority);
		if (exc != null) return (null, exc);
		return (obj, null);
	}
}