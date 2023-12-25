namespace echo.primary.core.h2tp;

public enum RfcStatusCode {
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
	internal static string ToString(RfcStatusCode ev) {
		return ev switch {
			RfcStatusCode.Continue => "Continue",
			RfcStatusCode.SwitchingProtocols => "Switching Protocols",
			RfcStatusCode.Processing => "Processing",
			RfcStatusCode.EarlyHints => "Early Hints",
			RfcStatusCode.OK => "OK",
			RfcStatusCode.Created => "Created",
			RfcStatusCode.Accepted => "Accepted",
			RfcStatusCode.NonAuthoritativeInformation => "Non-Authoritative Information",
			RfcStatusCode.NoContent => "No Content",
			RfcStatusCode.ResetContent => "Reset Content",
			RfcStatusCode.PartialContent => "Partial Content",
			RfcStatusCode.MultiStatus => "Multi-Status",
			RfcStatusCode.AlreadyReported => "Already Reported",
			RfcStatusCode.IMUsed => "IM Used",
			RfcStatusCode.MultipleChoices => "Multiple Choices",
			RfcStatusCode.MovedPermanently => "Moved Permanently",
			RfcStatusCode.Found => "Found",
			RfcStatusCode.SeeOther => "See Other",
			RfcStatusCode.NotModified => "Not Modified",
			RfcStatusCode.UseProxy => "Use Proxy",
			RfcStatusCode.TemporaryRedirect => "Temporary Redirect",
			RfcStatusCode.PermanentRedirect => "Permanent Redirect",
			RfcStatusCode.BadRequest => "Bad Request",
			RfcStatusCode.Unauthorized => "Unauthorized",
			RfcStatusCode.PaymentRequired => "Payment Required",
			RfcStatusCode.Forbidden => "Forbidden",
			RfcStatusCode.NotFound => "Not Found",
			RfcStatusCode.MethodNotAllowed => "Method Not Allowed",
			RfcStatusCode.NotAcceptable => "Not Acceptable",
			RfcStatusCode.ProxyAuthenticationRequired => "Proxy Authentication Required",
			RfcStatusCode.RequestTimeout => "Request Timeout",
			RfcStatusCode.Conflict => "Conflict",
			RfcStatusCode.Gone => "Gone",
			RfcStatusCode.LengthRequired => "Length Required",
			RfcStatusCode.PreconditionFailed => "Precondition Failed",
			RfcStatusCode.PayloadTooLarge => "Payload Too Large",
			RfcStatusCode.URITooLong => "URI Too Long",
			RfcStatusCode.UnsupportedMediaType => "Unsupported Media Type",
			RfcStatusCode.RangeNotSatisfiable => "Range Not Satisfiable",
			RfcStatusCode.ExpectationFailed => "Expectation Failed",
			RfcStatusCode.IAmATeapot => "I'm a teapot",
			RfcStatusCode.MisdirectedRequest => "Misdirected Request",
			RfcStatusCode.UnprocessableContent => "Unprocessable Content",
			RfcStatusCode.Locked => "Locked",
			RfcStatusCode.FailedDependency => "Failed Dependency",
			RfcStatusCode.TooEarly => "Too Early",
			RfcStatusCode.UpgradeRequired => "Upgrade Required",
			RfcStatusCode.PreconditionRequired => "Precondition Required",
			RfcStatusCode.TooManyRequests => "Too Many Requests",
			RfcStatusCode.RequestHeaderFieldsTooLarge => "Request Header Fields Too Large",
			RfcStatusCode.UnavailableForLegalReasons => "Unavailable For Legal Reasons",
			RfcStatusCode.InternalServerError => "Internal Server Error",
			RfcStatusCode.NotImplemented => "Not Implemented",
			RfcStatusCode.BadGateway => "Bad Gateway",
			RfcStatusCode.ServiceUnavailable => "Service Unavailable",
			RfcStatusCode.GatewayTimeout => "Gateway Timeout",
			RfcStatusCode.HTTPVersionNotSupported => "HTTP Version Not Supported",
			RfcStatusCode.VariantAlsoNegotiates => "Variant Also Negotiates",
			RfcStatusCode.InsufficientStorage => "Insufficient Storage",
			RfcStatusCode.LoopDetected => "Loop Detected",
			RfcStatusCode.NotExtended => "Not Extended",
			RfcStatusCode.NetworkAuthenticationRequired => "Network Authentication Required",
			_ => "Custom Code"
		};
	}
}