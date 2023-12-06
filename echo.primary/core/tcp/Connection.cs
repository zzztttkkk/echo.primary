using System.Net.Sockets;

namespace echo.primary.core.tcp;

public class Connection {
	protected Socket _socket;
	protected SslOptions? _sslOptions;
}