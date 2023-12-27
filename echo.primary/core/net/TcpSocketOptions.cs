using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using echo.primary.utils;

namespace echo.primary.core.net;

public class SslOptions {
	public string Filename { get; set; }
	public string? Password { get; set; } = null;
	public SslProtocols Protocols { get; set; } = SslProtocols.None;
	public RemoteCertificateValidationCallback? RemoteCertificateValidationCallback { get; set; } = null;

	public bool ClientCertificateRequired { get; set; } = false;
	public int HandshakeTimeoutMills { get; set; } = 0;

	private X509Certificate2? _certificate2;
	public X509Certificate2 Certificate => _certificate2 ??= new X509Certificate2(Filename, Password);

	public void Load() {
		_ = Certificate;
	}
}

public class TcpSocketOptions {
	public bool ReuseAddress { get; set; } = false;
	public bool ExclusiveAddressUse { get; set; } = false;
	public bool DualMode { get; set; } = false;
	public int Backlog { get; set; } = 128;
	public bool KeepAlive { get; set; } = false;
	public int KeepAliveTime { get; set; } = 0;
	public int KeepAliveInterval { get; set; } = 0;
	public int KeepAliveRetryCount { get; set; } = 0;
	public bool NoDelay { get; set; } = false;
	public uint BufferSize { get; set; } = 8192;

	public SslOptions? SslOptions { get; set; } = null;
}