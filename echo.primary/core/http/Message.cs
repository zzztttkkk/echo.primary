using echo.primary.core.io;

namespace echo.primary.core.http;

using MultiMap = Dictionary<string, List<string>>;

public class Message {
	protected readonly string[] flps;
	private MultiMap? herder = null;
	protected BytesBuffer? body = null;

	protected Message() {
		flps = new string[3];
	}

	public MultiMap? Header => herder;

	public MultiMap MustHeader() {
		herder ??= new();
		return herder;
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
}