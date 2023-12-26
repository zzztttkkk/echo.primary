using System.Diagnostics;

namespace echo.primary.core.h2tp;

public enum HttpRfcHeader {
/*<summary>

Acceptable instance-manipulations for the request.

</summary>

Example:

A-IM: feed

*/


	AIM,

/*<summary>

Media type(s) that is/are acceptable for the response. See Content negotiation.

</summary>

Example:

Accept: text/html

*/


	Accept,

/*<summary>

Character sets that are acceptable.

</summary>

Example:

Accept-Charset: utf-8

*/


	AcceptCharset,

/*<summary>

Acceptable version in time.

</summary>

Example:

Accept-Datetime: Thu, 31 May 2007 20:35:00 GMT

*/


	AcceptDatetime,

/*<summary>

List of acceptable encodings. See HTTP compression.

</summary>

Example:

Accept-Encoding: gzip, deflate

*/


	AcceptEncoding,

/*<summary>

List of acceptable human languages for response. See Content negotiation.

</summary>

Example:

Accept-Language: en-US

*/


	AcceptLanguage,

/*<summary>

Initiates a request for cross-origin resource sharing with Origin (below).

</summary>

Example:

Access-Control-Request-Method: GET

*/


	AccessControlRequestMethod,

	AccessControlRequestHeaders,

/*<summary>

Authentication credentials for HTTP authentication.

</summary>

Example:

Authorization: Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ==

*/


	Authorization,

/*<summary>

Used to specify directives that must be obeyed by all caching mechanisms along the request-response chain.

</summary>

Example:

Cache-Control: no-cache

*/


	CacheControl,

/*<summary>

Control options for the current connection and list of hop-by-hop request fields.
Must not be used with HTTP/2.

</summary>

Example:

Connection: keep-alive
Connection: Upgrade

*/


	Connection,

/*<summary>

The type of encoding used on the data. See HTTP compression.

</summary>

Example:

Content-Encoding: gzip

*/


	ContentEncoding,

/*<summary>

The length of the request body in octets (8-bit bytes).

</summary>

Example:

Content-Length: 348

*/


	ContentLength,

/*<summary>

A Base64-encoded binary MD5 sum of the content of the request body.

</summary>

Example:

Content-MD5: Q2hlY2sgSW50ZWdyaXR5IQ==

*/


	ContentMD5,

/*<summary>

The Media type of the body of the request (used with POST and PUT requests).

</summary>

Example:

Content-Type: application/x-www-form-urlencoded

*/


	ContentType,

/*<summary>

An HTTP cookie previously sent by the server with Set-Cookie (below).

</summary>

Example:

Cookie: $Version=1; Skin=new;

*/


	Cookie,

/*<summary>

The date and time at which the message was originated (in "HTTP-date" format as defined by RFC 9110: HTTP Semantics, section 5.6.7 "Date/Time Formats").

</summary>

Example:

Date: Tue, 15 Nov 1994 08:12:31 GMT

*/


	Date,

/*<summary>

Indicates that particular server behaviors are required by the client.

</summary>

Example:

Expect: 100-continue

*/


	Expect,

/*<summary>

Disclose original information of a client connecting to a web server through an HTTP proxy.

</summary>

Example:

Forwarded: for=192.0.2.60;proto=http;by=203.0.113.43 Forwarded: for=192.0.2.43, for=198.51.100.17

*/


	Forwarded,

/*<summary>

The email address of the user making the request.

</summary>

Example:

From: user@example.com

*/


	From,

/*<summary>

The domain name of the server (for virtual hosting), and the TCP port number on which the server is listening. The port number may be omitted if the port is the standard port for the service requested.
Mandatory since HTTP/1.1.
If the request is generated directly in HTTP/2, it should not be used.

</summary>

Example:

Host: en.wikipedia.org:8080
Host: en.wikipedia.org

*/


	Host,

/*<summary>

A request that upgrades from HTTP/1.1 to HTTP/2 MUST include exactly one HTTP2-Settings header field. The HTTP2-Settings header field is a connection-specific header field that includes parameters that govern the HTTP/2 connection, provided in anticipation of the server accepting the request to upgrade.

</summary>

Example:

HTTP2-Settings: token64

*/


	HTTP2Settings,

/*<summary>

Only perform the action if the client supplied entity matches the same entity on the server. This is mainly for methods like PUT to only update a resource if it has not been modified since the user last updated it.

</summary>

Example:

If-Match: "737060cd8c284d8af7ad3082f209582d"

*/


	IfMatch,

/*<summary>

Allows a 304 Not Modified to be returned if content is unchanged.

</summary>

Example:

If-Modified-Since: Sat, 29 Oct 1994 19:43:31 GMT

*/


	IfModifiedSince,

/*<summary>

Allows a 304 Not Modified to be returned if content is unchanged, see HTTP ETag.

</summary>

Example:

If-None-Match: "737060cd8c284d8af7ad3082f209582d"

*/


	IfNoneMatch,

/*<summary>

If the entity is unchanged, send me the part(s) that I am missing; otherwise, send me the entire new entity.

</summary>

Example:

If-Range: "737060cd8c284d8af7ad3082f209582d"

*/


	IfRange,

/*<summary>

Only send the response if the entity has not been modified since a specific time.

</summary>

Example:

If-Unmodified-Since: Sat, 29 Oct 1994 19:43:31 GMT

*/


	IfUnmodifiedSince,

/*<summary>

Limit the number of times the message can be forwarded through proxies or gateways.

</summary>

Example:

Max-Forwards: 10

*/


	MaxForwards,

/*<summary>

Initiates a request for cross-origin resource sharing (asks server for Access-Control-* response fields).

</summary>

Example:

Origin: http://www.example-social-network.com

*/


	Origin,

/*<summary>

Implementation-specific fields that may have various effects anywhere along the request-response chain.

</summary>

Example:

Pragma: no-cache

*/


	Pragma,

/*<summary>

Allows client to request that certain behaviors be employed by a server while processing a request.

</summary>

Example:

Prefer: return=representation

*/


	Prefer,

/*<summary>

Authorization credentials for connecting to a proxy.

</summary>

Example:

Proxy-Authorization: Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ==

*/


	ProxyAuthorization,

/*<summary>

Request only part of an entity.  Bytes are numbered from 0.  See Byte serving.

</summary>

Example:

Range: bytes=500-999

*/


	Range,

/*<summary>

This is the address of the previous web page from which a link to the currently requested page was followed. (The word "referrer" has been misspelled in the RFC as well as in most implementations to the point that it has become standard usage and is considered correct terminology)

</summary>

Example:

Referer: http://en.wikipedia.org/wiki/Main_Page

*/


	Referer,

/*<summary>

The transfer encodings the user agent is willing to accept: the same values as for the response header field Transfer-Encoding can be used, plus the "trailers" value (related to the "chunked" transfer method) to notify the server it expects to receive additional fields in the trailer after the last, zero-sized, chunk.
Only trailers is supported in HTTP/2.

</summary>

Example:

TE: trailers, deflate

*/


	TE,

/*<summary>

The Trailer general field value indicates that the given set of header fields is present in the trailer of a message encoded with chunked transfer coding.

</summary>

Example:

Trailer: Max-Forwards

*/


	Trailer,

/*<summary>

The form of encoding used to safely transfer the entity to the user. Currently defined methods are: chunked, compress, deflate, gzip, identity.
Must not be used with HTTP/2.

</summary>

Example:

Transfer-Encoding: chunked

*/


	TransferEncoding,

/*<summary>

The user agent string of the user agent.

</summary>

Example:

User-Agent: Mozilla/5.0 (X11; Linux x86_64; rv:12.0) Gecko/20100101 Firefox/12.0

*/


	UserAgent,

/*<summary>

Ask the server to upgrade to another protocol.
Must not be used in HTTP/2.

</summary>

Example:

Upgrade: h2c, HTTPS/1.3, IRC/6.9, RTA/x11, websocket

*/


	Upgrade,

/*<summary>

Informs the server of proxies through which the request was sent.

</summary>

Example:

Via: 1.0 fred, 1.1 example.com (Apache/1.1)

*/


	Via,

/*<summary>

A general warning about possible problems with the entity body.

</summary>

Example:

Warning: 199 Miscellaneous warning

*/


	Warning,

/*<summary>

Tells a server which (presumably in the middle of a HTTP -> HTTPS migration) hosts mixed content that the client would prefer redirection to HTTPS and can handle Content-Security-Policy: upgrade-insecure-requests
Must not be used with HTTP/2

</summary>

Example:

Upgrade-Insecure-Requests: 1

*/


	UpgradeInsecureRequests,

/*<summary>

Mainly used to identify Ajax requests (most JavaScript frameworks send this field with value of XMLHttpRequest); also identifies Android apps using WebView

</summary>

Example:

X-Requested-With: XMLHttpRequest

*/


	XRequestedWith,

/*<summary>

Requests a web application to disable their tracking of a user. This is Mozilla's version of the X-Do-Not-Track header field (since Firefox 4.0 Beta 11).  Safari and IE9 also have support for this field.

</summary>

Example:

DNT: 1 (Do Not Track Enabled)
DNT: 0 (Do Not Track Disabled)

*/


	DNT,

/*<summary>

A de facto standard for identifying the originating IP address of a client connecting to a web server through an HTTP proxy or load balancer. Superseded by Forwarded header.

</summary>

Example:

X-Forwarded-For: client1, proxy1, proxy2
X-Forwarded-For: 129.78.138.66, 129.78.64.103

*/


	XForwardedFor,

/*<summary>

A de facto standard for identifying the original host requested by the client in the Host HTTP request header, since the host name and/or port of the reverse proxy (load balancer) may differ from the origin server handling the request. Superseded by Forwarded header.

</summary>

Example:

X-Forwarded-Host: en.wikipedia.org:8080
X-Forwarded-Host: en.wikipedia.org

*/


	XForwardedHost,

/*<summary>

A de facto standard for identifying the originating protocol of an HTTP request, since a reverse proxy (or a load balancer) may communicate with a web server using HTTP even if the request to the reverse proxy is HTTPS.  An alternative form of the header (X-ProxyUser-Ip) is used by Google clients talking to Google servers. Superseded by Forwarded header.

</summary>

Example:

X-Forwarded-Proto: https

*/


	XForwardedProto,

/*<summary>

Non-standard header field used by Microsoft applications and load-balancers

</summary>

Example:

Front-End-Https: on

*/


	FrontEndHttps,

/*<summary>

Requests a web application to override the method specified in the request (typically POST) with the method given in the header field (typically PUT or DELETE). This can be used when a user agent or firewall prevents PUT or DELETE methods from being sent directly (note that this is either a bug in the software component, which ought to be fixed, or an intentional configuration, in which case bypassing it may be the wrong thing to do).

</summary>

Example:

X-HTTP-Method-Override: DELETE

*/


	XHttpMethodOverride,

/*<summary>

Allows easier parsing of the MakeModel/Firmware that is usually found in the User-Agent String of AT&T Devices

</summary>

Example:

X-Att-Deviceid: GT-P7320/P7320XXLPG

*/


	XATTDeviceId,

/*<summary>

Links to an XML file on the Internet with a full description and details about the device currently connecting. In the example to the right is an XML file for an AT&T Samsung Galaxy S2.

</summary>

Example:

x-wap-profile: http://wap.samsungmobile.com/uaprof/SGH-I777.xml

*/


	XWapProfile,

/*<summary>

Implemented as a misunderstanding of the HTTP specifications. Common because of mistakes in implementations of early HTTP versions. Has exactly the same functionality as standard Connection field.
Must not be used with HTTP/2.

</summary>

Example:

Proxy-Connection: keep-alive

*/


	ProxyConnection,

/*<summary>

Server-side deep packet inspection of a unique ID identifying customers of Verizon Wireless; also known as "perma-cookie" or "supercookie"

</summary>

Example:

X-UIDH: ...

*/


	XUIDH,

/*<summary>

Used to prevent cross-site request forgery. Alternative header names are: X-CSRFToken

</summary>

Example:

X-Csrf-Token: i8XNjC4b8KVok4uw5RftR38Wgp2BFwql

*/


	XCsrfToken,

/*<summary>

Correlates HTTP requests between a client and server.

</summary>

Example:

X-Request-ID: f058ebd6-02f7-4d3f-942e-904344e8cde5

*/


	XRequestID,

	XCorrelationID,

	CorrelationID,

/*<summary>

The Save-Data client hint request header available in Chrome, Opera, and Yandex browsers lets developers deliver lighter, faster applications to users who opt-in to data saving mode in their browser.

</summary>

Example:

Save-Data: on

*/


	SaveData,

/*<summary>

The Sec-GPC (Global Privacy Control) request header indicates whether the user consents to a website or service selling or sharing their personal information with third parties.

</summary>

Example:

Sec-GPC: 1

*/


	SecGPC,

/*<summary>

Requests HTTP Client Hints

</summary>

Example:

Accept-CH: UA, Platform

*/


	AcceptCH,

/*<summary>

Specifying which web sites can participate in cross-origin resource sharing

</summary>

Example:

Access-Control-Allow-Origin: *

*/


	AccessControlAllowOrigin,

	AccessControlAllowCredentials,

	AccessControlExposeHeaders,

	AccessControlMaxAge,

	AccessControlAllowMethods,

	AccessControlAllowHeaders,

/*<summary>

Specifies which patch document formats this server supports

</summary>

Example:

Accept-Patch: text/example;charset=utf-8

*/


	AcceptPatch,

/*<summary>

What partial content range types this server supports via byte serving

</summary>

Example:

Accept-Ranges: bytes

*/


	AcceptRanges,

/*<summary>

The age the object has been in a proxy cache in seconds

</summary>

Example:

Age: 12

*/


	Age,

/*<summary>

Valid methods for a specified resource. To be used for a 405 Method not allowed

</summary>

Example:

Allow: GET, HEAD

*/


	Allow,

/*<summary>

A server uses "Alt-Svc" header (meaning Alternative Services) to indicate that its resources can also be accessed at a different network location (host or port) or using a different protocol
When using HTTP/2, servers should instead send an ALTSVC frame.

</summary>

Example:

Alt-Svc: http/1.1="http2.example.com:8001"; ma=7200

*/


	AltSvc,

/*<summary>

Tells all caching mechanisms from server to client whether they may cache this object. It is measured in seconds

</summary>

Example:

Cache-Control: max-age=3600

*/


/*<summary>

Control options for the current connection and list of hop-by-hop response fields.
Must not be used with HTTP/2.

</summary>

Example:

Connection: close

*/


/*<summary>

An opportunity to raise a "File Download" dialogue box for a known MIME type with binary format or suggest a filename for dynamic content. Quotes are necessary with special characters.

</summary>

Example:

Content-Disposition: attachment; filename="fname.ext"

*/


	ContentDisposition,

/*<summary>

The type of encoding used on the data. See HTTP compression.

</summary>

Example:

Content-Encoding: gzip

*/


/*<summary>

The natural language or languages of the intended audience for the enclosed content

</summary>

Example:

Content-Language: da

*/


	ContentLanguage,

/*<summary>

The length of the response body in octets (8-bit bytes)

</summary>

Example:

Content-Length: 348

*/


/*<summary>

An alternate location for the returned data

</summary>

Example:

Content-Location: /index.htm

*/


	ContentLocation,

/*<summary>

A Base64-encoded binary MD5 sum of the content of the response

</summary>

Example:

Content-MD5: Q2hlY2sgSW50ZWdyaXR5IQ==

*/


/*<summary>

Where in a full body message this partial message belongs

</summary>

Example:

Content-Range: bytes 21010-47021/47022

*/


	ContentRange,

/*<summary>

The MIME type of this content

</summary>

Example:

Content-Type: text/html; charset=utf-8

*/


/*<summary>

The date and time that the message was sent (in "HTTP-date" format as defined by RFC 9110)

</summary>

Example:

Date: Tue, 15 Nov 1994 08:12:31 GMT

*/


/*<summary>

Specifies the delta-encoding entity tag of the response.

</summary>

Example:

Delta-Base: "abc"

*/


	DeltaBase,

/*<summary>

An identifier for a specific version of a resource, often a message digest

</summary>

Example:

ETag: "737060cd8c284d8af7ad3082f209582d"

*/


	ETag,

/*<summary>

Gives the date/time after which the response is considered stale (in "HTTP-date" format as defined by RFC 9110)

</summary>

Example:

Expires: Thu, 01 Dec 1994 16:00:00 GMT

*/


	Expires,

/*<summary>

Instance-manipulations applied to the response.

</summary>

Example:

IM: feed

*/


	IM,

/*<summary>

The last modified date for the requested object (in "HTTP-date" format as defined by RFC 9110)

</summary>

Example:

Last-Modified: Tue, 15 Nov 1994 12:45:26 GMT

*/


	LastModified,

/*<summary>

Used to express a typed relationship with another resource, where the relation type is defined by RFC 5988

</summary>

Example:

Link: </feed>; rel="alternate"

*/


	Link,

/*<summary>

Used in redirection, or when a new resource has been created.

</summary>

Example:

Example 1: Location: http://www.w3.org/pub/WWW/People.html
Example 2: Location: /pub/WWW/People.html

*/


	Location,

/*<summary>

This field is supposed to set P3P policy, in the form of P3P:CP="your_compact_policy". However, P3P did not take off, most browsers have never fully implemented it, a lot of websites set this field with fake policy text, that was enough to fool browsers the existence of P3P policy and grant permissions for third party cookies.

</summary>

Example:

P3P: CP="This is not a P3P policy! See https://en.wikipedia.org/wiki/Special:CentralAutoLogin/P3P for more info."

*/


	P3P,

/*<summary>

Implementation-specific fields that may have various effects anywhere along the request-response chain.

</summary>

Example:

Pragma: no-cache

*/


/*<summary>

Indicates which Prefer tokens were honored by the server and applied to the processing of the request.

</summary>

Example:

Preference-Applied: return=representation

*/


	PreferenceApplied,

/*<summary>

Request authentication to access the proxy.

</summary>

Example:

Proxy-Authenticate: Basic

*/


	ProxyAuthenticate,

/*<summary>

HTTP Public Key Pinning, announces hash of website's authentic TLS certificate

</summary>

Example:

Public-Key-Pins: max-age=2592000; pin-sha256="E9CZ9INDbd+2eRQozYqqbQ2yXLVKB9+xcprMF+44U1g=";

*/


	PublicKeyPins,

/*<summary>

If an entity is temporarily unavailable, this instructs the client to try again later. Value could be a specified period of time (in seconds) or a HTTP-date.

</summary>

Example:

Example 1: Retry-After: 120
Example 2: Retry-After: Fri, 07 Nov 2014 23:59:59 GMT

*/


	RetryAfter,

/*<summary>

A name for the server

</summary>

Example:

Server: Apache/2.4.1 (Unix)

*/


	Server,

/*<summary>

An HTTP cookie

</summary>

Example:

Set-Cookie: UserID=JohnDoe; Max-Age=3600; Version=1

*/


	SetCookie,

/*<summary>

A HSTS Policy informing the HTTP client how long to cache the HTTPS only policy and whether this applies to subdomains.

</summary>

Example:

Strict-Transport-Security: max-age=16070400; includeSubDomains

*/


	StrictTransportSecurity,

/*<summary>

The Trailer general field value indicates that the given set of header fields is present in the trailer of a message encoded with chunked transfer coding.

</summary>

Example:

Trailer: Max-Forwards

*/


/*<summary>

The form of encoding used to safely transfer the entity to the user. Currently defined methods are: chunked, compress, deflate, gzip, identity.
Must not be used with HTTP/2.

</summary>

Example:

Transfer-Encoding: chunked

*/


/*<summary>

Tracking Status header, value suggested to be sent in response to a DNT(do-not-track), possible values:
"!" — under construction
"?" — dynamic
"G" — gateway to multiple parties
"N" — not tracking
"T" — tracking
"C" — tracking with consent
"P" — tracking only if consented
"D" — disregarding DNT
"U" — updated

</summary>

Example:

Tk: ?

*/


	Tk,

/*<summary>

Ask the client to upgrade to another protocol.
Must not be used in HTTP/2

</summary>

Example:

Upgrade: h2c, HTTPS/1.3, IRC/6.9, RTA/x11, websocket

*/


/*<summary>

Tells downstream proxies how to match future request headers to decide whether the cached response can be used rather than requesting a fresh one from the origin server.

</summary>

Example:

Example 1: Vary: *
Example 2: Vary: Accept-Language

*/


	Vary,

/*<summary>

Informs the client of proxies through which the response was sent.

</summary>

Example:

Via: 1.0 fred, 1.1 example.com (Apache/1.1)

*/


/*<summary>

A general warning about possible problems with the entity body.

</summary>

Example:

Warning: 199 Miscellaneous warning

*/


/*<summary>

Indicates the authentication scheme that should be used to access the requested entity.

</summary>

Example:

WWW-Authenticate: Basic

*/


	WWWAuthenticate,

/*<summary>

Clickjacking protection: deny - no rendering within a frame, sameorigin - no rendering if origin mismatch, allow-from - allow from specified location, allowall - non-standard, allow from any location

</summary>

Example:

X-Frame-Options: deny

*/


	XFrameOptions,

/*<summary>

Content Security Policy definition.

</summary>

Example:

X-WebKit-CSP: default-src 'self'

*/


	ContentSecurityPolicy,

	XContentSecurityPolicy,

	XWebKitCSP,

/*<summary>

Notify to prefer to enforce Certificate Transparency.

</summary>

Example:

Expect-CT: max-age=604800, enforce, report-uri="https://example.example/report"

*/


	ExpectCT,

/*<summary>

Used to configure network request logging.

</summary>

Example:

NEL: { "report_to": "name_of_reporting_group", "max_age": 12345, "include_subdomains": false, "success_fraction": 0.0, "failure_fraction": 1.0 }

*/


	NEL,

/*<summary>

To allow or disable different features or APIs of the browser.

</summary>

Example:

Permissions-Policy: fullscreen=(), camera=(), microphone=(), geolocation=(), interest-cohort=()

*/


	PermissionsPolicy,

/*<summary>

Used in redirection, or when a new resource has been created.  This refresh redirects after 5 seconds. Header extension introduced by Netscape and supported by most web browsers. Defined by HTML Standard

</summary>

Example:

Refresh: 5; url=http://www.w3.org/pub/WWW/People.html

*/


	Refresh,

/*<summary>

Instructs the user agent to store reporting endpoints for an origin.

</summary>

Example:

Report-To: { "group": "csp-endpoint", "max_age": 10886400, "endpoints":  }

*/


	ReportTo,

/*<summary>

CGI header field specifying the status of the HTTP response. Normal HTTP responses use a separate "Status-Line" instead, defined by RFC 9110.

</summary>

Example:

Status: 200 OK

*/


	Status,

/*<summary>

The Timing-Allow-Origin response header specifies origins that are allowed to see values of attributes retrieved via features of the Resource Timing API, which would otherwise be reported as zero due to cross-origin restrictions.

</summary>

Example:

Timing-Allow-Origin: *
Timing-Allow-Origin: <origin>*

*/


	TimingAllowOrigin,

/*<summary>

Provide the duration of the audio or video in seconds; only supported by Gecko browsers

</summary>

Example:

X-Content-Duration: 42.666

*/


	XContentDuration,

/*<summary>

The only defined value, "nosniff", prevents Internet Explorer from MIME-sniffing a response away from the declared content-type. This also applies to Google Chrome, when downloading extensions.

</summary>

Example:

X-Content-Type-Options: nosniff

*/


	XContentTypeOptions,

/*<summary>

Specifies the technology (e.g. ASP.NET, PHP, JBoss) supporting the web application (version details are often in X-Runtime, X-Version, or X-AspNet-Version)

</summary>

Example:

X-Powered-By: PHP/5.4.0

*/


	XPoweredBy,

/*<summary>

Specifies the component that is responsible for a particular redirect.

</summary>

Example:

X-Redirect-By: WordPressX-Redirect-By: Polylang

*/


	XRedirectBy,

/*<summary>

Correlates HTTP requests between a client and server.

</summary>

Example:

X-Request-ID: f058ebd6-02f7-4d3f-942e-904344e8cde5

*/


/*<summary>

Recommends the preferred rendering engine (often a backward-compatibility mode) to use to display the content. Also used to activate Chrome Frame in Internet Explorer. In HTML Standard, only the IE=edge value is defined.

</summary>

Example:

X-UA-Compatible: IE=edgeX-UA-Compatible: IE=EmulateIE7X-UA-Compatible: Chrome=1

*/


	XUACompatible,

/*<summary>

Cross-site scripting (XSS) filter

</summary>

Example:

X-XSS-Protection: 1; mode=block

*/


	XXSSProtection,
}

internal static class HeaderToString {
	internal static string ToString(HttpRfcHeader ev) {
		return ev switch {
			HttpRfcHeader.AIM => "a-im",
			HttpRfcHeader.Accept => "accept",
			HttpRfcHeader.AcceptCharset => "accept-charset",
			HttpRfcHeader.AcceptDatetime => "accept-datetime",
			HttpRfcHeader.AcceptEncoding => "accept-encoding",
			HttpRfcHeader.AcceptLanguage => "accept-language",
			HttpRfcHeader.AccessControlRequestMethod => "access-control-request-method",
			HttpRfcHeader.AccessControlRequestHeaders => "access-control-request-headers",
			HttpRfcHeader.Authorization => "authorization",
			HttpRfcHeader.CacheControl => "cache-control",
			HttpRfcHeader.Connection => "connection",
			HttpRfcHeader.ContentEncoding => "content-encoding",
			HttpRfcHeader.ContentLength => "content-length",
			HttpRfcHeader.ContentMD5 => "content-md5",
			HttpRfcHeader.ContentType => "content-type",
			HttpRfcHeader.Cookie => "cookie",
			HttpRfcHeader.Date => "date",
			HttpRfcHeader.Expect => "expect",
			HttpRfcHeader.Forwarded => "forwarded",
			HttpRfcHeader.From => "from",
			HttpRfcHeader.Host => "host",
			HttpRfcHeader.HTTP2Settings => "http2-settings",
			HttpRfcHeader.IfMatch => "if-match",
			HttpRfcHeader.IfModifiedSince => "if-modified-since",
			HttpRfcHeader.IfNoneMatch => "if-none-match",
			HttpRfcHeader.IfRange => "if-range",
			HttpRfcHeader.IfUnmodifiedSince => "if-unmodified-since",
			HttpRfcHeader.MaxForwards => "max-forwards",
			HttpRfcHeader.Origin => "origin",
			HttpRfcHeader.Pragma => "pragma",
			HttpRfcHeader.Prefer => "prefer",
			HttpRfcHeader.ProxyAuthorization => "proxy-authorization",
			HttpRfcHeader.Range => "range",
			HttpRfcHeader.Referer => "referer",
			HttpRfcHeader.TE => "te",
			HttpRfcHeader.Trailer => "trailer",
			HttpRfcHeader.TransferEncoding => "transfer-encoding",
			HttpRfcHeader.UserAgent => "user-agent",
			HttpRfcHeader.Upgrade => "upgrade",
			HttpRfcHeader.Via => "via",
			HttpRfcHeader.Warning => "warning",
			HttpRfcHeader.UpgradeInsecureRequests => "upgrade-insecure-requests",
			HttpRfcHeader.XRequestedWith => "x-requested-with",
			HttpRfcHeader.DNT => "dnt",
			HttpRfcHeader.XForwardedFor => "x-forwarded-for",
			HttpRfcHeader.XForwardedHost => "x-forwarded-host",
			HttpRfcHeader.XForwardedProto => "x-forwarded-proto",
			HttpRfcHeader.FrontEndHttps => "front-end-https",
			HttpRfcHeader.XHttpMethodOverride => "x-http-method-override",
			HttpRfcHeader.XATTDeviceId => "x-att-deviceid",
			HttpRfcHeader.XWapProfile => "x-wap-profile",
			HttpRfcHeader.ProxyConnection => "proxy-connection",
			HttpRfcHeader.XUIDH => "x-uidh",
			HttpRfcHeader.XCsrfToken => "x-csrf-token",
			HttpRfcHeader.XRequestID => "x-request-id",
			HttpRfcHeader.XCorrelationID => "x-correlation-id",
			HttpRfcHeader.CorrelationID => "correlation-id",
			HttpRfcHeader.SaveData => "save-data",
			HttpRfcHeader.SecGPC => "sec-gpc",
			HttpRfcHeader.AcceptCH => "accept-ch",
			HttpRfcHeader.AccessControlAllowOrigin => "access-control-allow-origin",
			HttpRfcHeader.AccessControlAllowCredentials => "access-control-allow-credentials",
			HttpRfcHeader.AccessControlExposeHeaders => "access-control-expose-headers",
			HttpRfcHeader.AccessControlMaxAge => "access-control-max-age",
			HttpRfcHeader.AccessControlAllowMethods => "access-control-allow-methods",
			HttpRfcHeader.AccessControlAllowHeaders => "access-control-allow-headers",
			HttpRfcHeader.AcceptPatch => "accept-patch",
			HttpRfcHeader.AcceptRanges => "accept-ranges",
			HttpRfcHeader.Age => "age",
			HttpRfcHeader.Allow => "allow",
			HttpRfcHeader.AltSvc => "alt-svc",
			HttpRfcHeader.ContentDisposition => "content-disposition",
			HttpRfcHeader.ContentLanguage => "content-language",
			HttpRfcHeader.ContentLocation => "content-location",
			HttpRfcHeader.ContentRange => "content-range",
			HttpRfcHeader.DeltaBase => "delta-base",
			HttpRfcHeader.ETag => "etag",
			HttpRfcHeader.Expires => "expires",
			HttpRfcHeader.IM => "im",
			HttpRfcHeader.LastModified => "last-modified",
			HttpRfcHeader.Link => "link",
			HttpRfcHeader.Location => "location",
			HttpRfcHeader.P3P => "p3p",
			HttpRfcHeader.PreferenceApplied => "preference-applied",
			HttpRfcHeader.ProxyAuthenticate => "proxy-authenticate",
			HttpRfcHeader.PublicKeyPins => "public-key-pins",
			HttpRfcHeader.RetryAfter => "retry-after",
			HttpRfcHeader.Server => "server",
			HttpRfcHeader.SetCookie => "set-cookie",
			HttpRfcHeader.StrictTransportSecurity => "strict-transport-security",
			HttpRfcHeader.Tk => "tk",
			HttpRfcHeader.Vary => "vary",
			HttpRfcHeader.WWWAuthenticate => "www-authenticate",
			HttpRfcHeader.XFrameOptions => "x-frame-options",
			HttpRfcHeader.ContentSecurityPolicy => "content-security-policy",
			HttpRfcHeader.XContentSecurityPolicy => "x-content-security-policy",
			HttpRfcHeader.XWebKitCSP => "x-webkit-csp",
			HttpRfcHeader.ExpectCT => "expect-ct",
			HttpRfcHeader.NEL => "nel",
			HttpRfcHeader.PermissionsPolicy => "permissions-policy",
			HttpRfcHeader.Refresh => "refresh",
			HttpRfcHeader.ReportTo => "report-to",
			HttpRfcHeader.Status => "status",
			HttpRfcHeader.TimingAllowOrigin => "timing-allow-origin",
			HttpRfcHeader.XContentDuration => "x-content-duration",
			HttpRfcHeader.XContentTypeOptions => "x-content-type-options",
			HttpRfcHeader.XPoweredBy => "x-powered-by",
			HttpRfcHeader.XRedirectBy => "x-redirect-by",
			HttpRfcHeader.XUACompatible => "x-ua-compatible",
			HttpRfcHeader.XXSSProtection => "x-xss-protection",
			_ => throw new UnreachableException()
		};
	}
}