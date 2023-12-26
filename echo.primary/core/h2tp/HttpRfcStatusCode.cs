namespace echo.primary.core.h2tp;

public enum HttpRfcStatusCode {
	Continue = 100,

	SwitchingProtocols = 101,

	Processing = 102,

	EarlyHints = 103,

	OK = 200,

	Created = 201,

	Accepted = 202,

	NonAuthoritativeInformation = 203,

	NoContent = 204,

	ResetContent = 205,

	PartialContent = 206,

	MultiStatus = 207,

	AlreadyReported = 208,

	IMUsed = 226,

	MultipleChoices = 300,

	MovedPermanently = 301,

	Found = 302,

	SeeOther = 303,

	NotModified = 304,

	UseProxy = 305,

	TemporaryRedirect = 307,

	PermanentRedirect = 308,

	BadRequest = 400,

	Unauthorized = 401,

	PaymentRequired = 402,

	Forbidden = 403,

	NotFound = 404,

	MethodNotAllowed = 405,

	NotAcceptable = 406,

	ProxyAuthenticationRequired = 407,

	RequestTimeout = 408,

	Conflict = 409,

	Gone = 410,

	LengthRequired = 411,

	PreconditionFailed = 412,

	PayloadTooLarge = 413,

	URITooLong = 414,

	UnsupportedMediaType = 415,

	RangeNotSatisfiable = 416,

	ExpectationFailed = 417,

	IAmATeapot = 418,

	MisdirectedRequest = 421,

	UnprocessableContent = 422,

	Locked = 423,

	FailedDependency = 424,

	TooEarly = 425,

	UpgradeRequired = 426,

	PreconditionRequired = 428,

	TooManyRequests = 429,

	RequestHeaderFieldsTooLarge = 431,

	UnavailableForLegalReasons = 451,

	InternalServerError = 500,

	NotImplemented = 501,

	BadGateway = 502,

	ServiceUnavailable = 503,

	GatewayTimeout = 504,

	HTTPVersionNotSupported = 505,

	VariantAlsoNegotiates = 506,

	InsufficientStorage = 507,

	LoopDetected = 508,

	NotExtended = 510,

	NetworkAuthenticationRequired = 511,
}

internal static class StatusToString {
	internal static string ToString(HttpRfcStatusCode ev) {
		return ev switch {
			HttpRfcStatusCode.Continue => "Continue",
			HttpRfcStatusCode.SwitchingProtocols => "Switching Protocols",
			HttpRfcStatusCode.Processing => "Processing",
			HttpRfcStatusCode.EarlyHints => "Early Hints",
			HttpRfcStatusCode.OK => "OK",
			HttpRfcStatusCode.Created => "Created",
			HttpRfcStatusCode.Accepted => "Accepted",
			HttpRfcStatusCode.NonAuthoritativeInformation => "Non-Authoritative Information",
			HttpRfcStatusCode.NoContent => "No Content",
			HttpRfcStatusCode.ResetContent => "Reset Content",
			HttpRfcStatusCode.PartialContent => "Partial Content",
			HttpRfcStatusCode.MultiStatus => "Multi-Status",
			HttpRfcStatusCode.AlreadyReported => "Already Reported",
			HttpRfcStatusCode.IMUsed => "IM Used",
			HttpRfcStatusCode.MultipleChoices => "Multiple Choices",
			HttpRfcStatusCode.MovedPermanently => "Moved Permanently",
			HttpRfcStatusCode.Found => "Found",
			HttpRfcStatusCode.SeeOther => "See Other",
			HttpRfcStatusCode.NotModified => "Not Modified",
			HttpRfcStatusCode.UseProxy => "Use Proxy",
			HttpRfcStatusCode.TemporaryRedirect => "Temporary Redirect",
			HttpRfcStatusCode.PermanentRedirect => "Permanent Redirect",
			HttpRfcStatusCode.BadRequest => "Bad Request",
			HttpRfcStatusCode.Unauthorized => "Unauthorized",
			HttpRfcStatusCode.PaymentRequired => "Payment Required",
			HttpRfcStatusCode.Forbidden => "Forbidden",
			HttpRfcStatusCode.NotFound => "Not Found",
			HttpRfcStatusCode.MethodNotAllowed => "Method Not Allowed",
			HttpRfcStatusCode.NotAcceptable => "Not Acceptable",
			HttpRfcStatusCode.ProxyAuthenticationRequired => "Proxy Authentication Required",
			HttpRfcStatusCode.RequestTimeout => "Request Timeout",
			HttpRfcStatusCode.Conflict => "Conflict",
			HttpRfcStatusCode.Gone => "Gone",
			HttpRfcStatusCode.LengthRequired => "Length Required",
			HttpRfcStatusCode.PreconditionFailed => "Precondition Failed",
			HttpRfcStatusCode.PayloadTooLarge => "Payload Too Large",
			HttpRfcStatusCode.URITooLong => "URI Too Long",
			HttpRfcStatusCode.UnsupportedMediaType => "Unsupported Media Type",
			HttpRfcStatusCode.RangeNotSatisfiable => "Range Not Satisfiable",
			HttpRfcStatusCode.ExpectationFailed => "Expectation Failed",
			HttpRfcStatusCode.IAmATeapot => "I'm a teapot",
			HttpRfcStatusCode.MisdirectedRequest => "Misdirected Request",
			HttpRfcStatusCode.UnprocessableContent => "Unprocessable Content",
			HttpRfcStatusCode.Locked => "Locked",
			HttpRfcStatusCode.FailedDependency => "Failed Dependency",
			HttpRfcStatusCode.TooEarly => "Too Early",
			HttpRfcStatusCode.UpgradeRequired => "Upgrade Required",
			HttpRfcStatusCode.PreconditionRequired => "Precondition Required",
			HttpRfcStatusCode.TooManyRequests => "Too Many Requests",
			HttpRfcStatusCode.RequestHeaderFieldsTooLarge => "Request Header Fields Too Large",
			HttpRfcStatusCode.UnavailableForLegalReasons => "Unavailable For Legal Reasons",
			HttpRfcStatusCode.InternalServerError => "Internal Server Error",
			HttpRfcStatusCode.NotImplemented => "Not Implemented",
			HttpRfcStatusCode.BadGateway => "Bad Gateway",
			HttpRfcStatusCode.ServiceUnavailable => "Service Unavailable",
			HttpRfcStatusCode.GatewayTimeout => "Gateway Timeout",
			HttpRfcStatusCode.HTTPVersionNotSupported => "HTTP Version Not Supported",
			HttpRfcStatusCode.VariantAlsoNegotiates => "Variant Also Negotiates",
			HttpRfcStatusCode.InsufficientStorage => "Insufficient Storage",
			HttpRfcStatusCode.LoopDetected => "Loop Detected",
			HttpRfcStatusCode.NotExtended => "Not Extended",
			HttpRfcStatusCode.NetworkAuthenticationRequired => "Network Authentication Required",
			_ => "Custom Code"
		};
	}
}