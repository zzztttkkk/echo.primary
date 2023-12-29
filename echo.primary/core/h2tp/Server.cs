using echo.primary.core.net;
using echo.primary.utils;

namespace echo.primary.core.h2tp;

public class ServerOptions {
	[Toml(Optional = true, Aliases = new[] { "tcp" })]
	public TcpSocketOptions TcpSocketOptions { get; set; } = new();

	[Toml(Optional = true, Aliases = new[] { "ver1", "version1" })]
	public Version1Options Version1Options { get; set; } = new();
}

public class Server(ServerOptions options) : TcpServer(options.TcpSocketOptions) { }