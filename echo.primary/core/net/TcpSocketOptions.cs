using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace echo.primary.core.net;

public record SslOptions(
	string Filename,
	string? Password = null,
	SslProtocols Protocols = SslProtocols.Tls13,
	RemoteCertificateValidationCallback? RemoteCertificateValidationCallback = null,
	bool ClientCertificateRequired = false
) {
	private X509Certificate2? _certificate2;
	public X509Certificate2 Certificate => _certificate2 ??= new X509Certificate2(Filename, Password);

	public void Load() {
		_ = Certificate;
	}
}

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