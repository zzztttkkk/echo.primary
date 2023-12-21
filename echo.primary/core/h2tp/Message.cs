using System.Net;
using System.Net.Http.Headers;
using echo.primary.core.io;

namespace echo.primary.core.h2tp;

using MultiMap = Dictionary<string, List<string>>;

enum MessageReadStatus {
	None = 0,
	FL1_OK,
	FL2_OK,
	FL3_OK,
	HEADER_OK,
	BODY_OK,
}

public class Message {
	internal readonly string[] flps;
	internal HttpHeaders? herder = null;
	internal BytesBuffer? body = null;

	protected Message() {
		flps = new string[3];
	}
}

public class Request : Message {
	private Uri? _uri;

	public string Method {
		get => flps[0];
		set => flps[0] = value;
	}

	public Uri Uri {
		get {
			_uri ??= new Uri(flps[1]);
			return _uri;
		}
	}

	public string Version {
		get => flps[2];
		set => flps[2] = value;
	}

	public HttpRequestHeaders Headers {
		get {
			// todo simple http headers
			herder ??= (HttpRequestHeaders)Activator.CreateInstance(typeof(HttpRequestHeaders), nonPublic: true)!;
			return (HttpRequestHeaders)herder;
		}
	}
}