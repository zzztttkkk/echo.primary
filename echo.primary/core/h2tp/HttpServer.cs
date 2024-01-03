using echo.primary.core.net;
using echo.primary.utils;

namespace echo.primary.core.h2tp;

public class ServerOptions {
	[Toml(Optional = true)] public string Host { get; set; } = "127.0.0.1";

	[Toml(Optional = true)] public ushort Port { get; set; } = 8080;

	[Toml(Optional = true, Aliases = new[] { "tcp" })]
	public TcpSocketOptions TcpSocketOptions { get; set; } = new();

	[Toml(Optional = true, Aliases = new[] { "http" })]
	public HttpOptions HttpOptions { get; set; } = new();


	[Toml(Optional = true, Aliases = new[] { "fs", "static" })]
	public FsOptions FsOptions { get; set; } = new();
}

public class HttpServer(ServerOptions options, string name = "HttpServer") : TcpServer(options.TcpSocketOptions, name) {
	public Task Start(IHandler handler) {
		return base.Start(
			options.Host,
			options.Port,
			() => new Version1Protocol(handler, options.HttpOptions)
		);
	}
}