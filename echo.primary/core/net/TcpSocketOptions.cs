using System.Security.Authentication;

namespace echo.primary.core.net;

public record SslOptions(
	string Filename,
	string? Password = null,
	SslProtocols? Protocols = null
);

public record TcpSocketOptions(
	bool ReuseAddress = false,
	bool ExclusiveAddressUse = false,
	bool DualMode = false,
	int Backlog = 128,
	bool KeepAlive = false,
	int KeepAliveTime = 0,
	int KeepAliveInterval = 0,
	int KeepAliveRetryCount = 0,
	bool NoDelay = false,
	uint ReceiveBufferSize = 10240,
	uint SendBufferSize = 10240,
	SslOptions? SslOptions = null
);