using System.Web;

namespace echo.primary.core.h2tp;

public class Request : Message {
	public string Method {
		get => Flps[0];
		set => Flps[0] = value;
	}

	private int? _qidx;

	private int QIdx {
		get {
			if (_qidx != null) return _qidx.Value;
			_qidx = Flps[1].IndexOf('?');
			return _qidx.Value;
		}
	}

	public string RawPath {
		get => Flps[1];
		set {
			Flps[1] = value;
			_qidx = null;
			_query.Clear();
			_queryParsed = false;
		}
	}

	public ReadOnlySpan<char> Path => QIdx < 0 ? Flps[1].AsSpan() : Flps[1].AsSpan()[..QIdx];
	public ReadOnlySpan<char> QueryString => QIdx < 0 ? "" : Flps[1].AsSpan()[(QIdx + 1)..];

	private readonly MultiMap _query = new();
	private bool _queryParsed;

	public MultiMap QueryParams {
		get {
			if (_queryParsed) return _query;
			_queryParsed = true;

			var tmp = QueryString;
			while (true) {
				var idx = tmp.IndexOf('&');
				if (idx < 0) {
					ParsePair(tmp);
					break;
				}

				ParsePair(tmp[..idx]);
				tmp = tmp[(idx + 1)..];
			}

			return _query;

			void ParsePair(ReadOnlySpan<char> txt) {
				var idx = txt.IndexOf('=');
				if (idx < 0) {
					_query.Add(Helper.UrlDecode(txt), "");
				}
				else {
					_query.Add(Helper.UrlDecode(txt[..idx]), Helper.UrlDecode(txt[(idx + 1)..]));
				}
			}
		}
	}

	public bool IsGet => Method == RfcMethod.GET.ToString();
	public bool IsPost => Method == RfcMethod.POST.ToString();
	public bool IsHead => Method == RfcMethod.HEAD.ToString();
	public bool IsPut => Method == RfcMethod.PUT.ToString();
	public bool IsDelete => Method == RfcMethod.DELETE.ToString();
	public bool IsConnect => Method == RfcMethod.CONNECT.ToString();
	public bool IsOptions => Method == RfcMethod.OPTIONS.ToString();
	public bool IsTrace => Method == RfcMethod.TRACE.ToString();
	public bool IsPatch => Method == RfcMethod.PATCH.ToString();
	public string ProtocolVersion => Flps[2];

	public Headers Headers {
		get {
			Herders ??= new Headers();
			return Herders;
		}
	}

	internal new void Reset() {
		base.Reset();
	}
}