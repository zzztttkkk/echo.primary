using System.Diagnostics.CodeAnalysis;

namespace echo.primary.core.h2tp;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum RfcMethod {
	GET,
	HEAD,
	POST,
	PUT,
	DELETE,
	CONNECT,
	OPTIONS,
	TRACE,
	PATCH
}