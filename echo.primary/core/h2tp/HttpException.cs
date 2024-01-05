namespace echo.primary.core.h2tp;

public class HttpException : Exception {
	public int Code { get; }

	public HttpException(RfcStatusCode code, string? msg = null) : base(msg) {
		Code = (int)code;
	}

	public HttpException(int code, string? msg = null) : base(msg) {
		Code = code;
	}
}