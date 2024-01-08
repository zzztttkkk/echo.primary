using Uri = echo.primary.utils.Uri;

namespace echo.primary.core.h2tp;

public class Request : Message {
	public string Method {
		get => Flps[0];
		set => Flps[0] = value;
	}

	internal Uri? InnerUri;

	public Uri Uri {
		get {
			if (InnerUri != null) return InnerUri;

			var (v, e) = Uri.Parse(Flps[1]);
			if (e != null) throw new Exception(e);
			InnerUri = v!;
			return InnerUri;
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