using echo.primary.core.net;
using echo.primary.utils;

namespace echo.primary.core.h2tp;

public class ServerOptions {
	[Toml(Optional = true, Aliases = new[] { "tcp" })] public TcpSocketOptions TcpSocketOptions { get; set; } = new();

	[Toml(Optional = true, Aliases = new[] { "ver11", "version11" })]
	public Version11Options Version11Options { get; set; } = new();
}

public class Server : TcpServer {
}