using echo.primary.core.net;
using echo.primary.utils;

namespace echo.primary.core.h2tp;

public class ServerOptions {
	[Toml(Optional = true, Aliases = new[] { "tcp" })]
	public TcpSocketOptions TcpSocketOptions { get; set; } = new();

	[Toml(Optional = true, ParserType = typeof(TomlParsers.ByteSizeParser))]
	public int MaxFirstLineBytesSize { get; set; } = 4096;

	[Toml(Optional = true, ParserType = typeof(TomlParsers.ByteSizeParser))]
	public int MaxHeaderLineBytesSize { get; set; } = 4096;

	[Toml(Optional = true, ParserType = typeof(TomlParsers.ByteSizeParser))]
	public int MaxHeadersCount { get; set; } = 1024;

	[Toml(Optional = true, ParserType = typeof(TomlParsers.ByteSizeParser))]
	public int MaxBodyBytesSize { get; set; } = 1024 * 1024;

	[Toml(Optional = true, ParserType = typeof(TomlParsers.DurationParser))]
	public int ReadTimeout { get; set; } = 10_000;

	[Toml(Optional = true, ParserType = typeof(TomlParsers.DurationParser))]
	public int HandleTimeout { get; set; } = 0;

	[Toml(Optional = true)] public bool EnableCompression { get; set; } = false;

	[Toml(Optional = true, ParserType = typeof(TomlParsers.ByteSizeParser))]
	public int MinCompressionSize { get; set; } = 1024;

	[Toml(Optional = true, ParserType = typeof(TomlParsers.ByteSizeParser))]
	public int StreamReadBufferSize { get; set; } = 4096;
}

public class HttpServer(ServerOptions options) : TcpServer(options.TcpSocketOptions) {
}