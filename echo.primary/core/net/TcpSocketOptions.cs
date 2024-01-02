using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using echo.primary.utils;

namespace echo.primary.core.net;

public class SslOptions {
	public string Filename { get; set; } = "";

	[Toml(Optional = true)] public string? Password { get; set; } = null;
	public SslProtocols Protocols { get; set; } = SslProtocols.None;

	[Toml(Ignored = true)]
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
	[Toml(Optional = true)] public bool ReuseAddress { get; set; } = false;
	[Toml(Optional = true)] public bool ExclusiveAddressUse { get; set; } = false;
	[Toml(Optional = true)] public bool DualMode { get; set; } = false;
	[Toml(Optional = true)] public int Backlog { get; set; } = 128;
	[Toml(Optional = true)] public bool KeepAlive { get; set; } = false;
	[Toml(Optional = true)] public int KeepAliveTime { get; set; } = 0;
	[Toml(Optional = true)] public int KeepAliveInterval { get; set; } = 0;
	[Toml(Optional = true)] public int KeepAliveRetryCount { get; set; } = 0;
	[Toml(Optional = true)] public bool NoDelay { get; set; } = false;

	[Toml(Optional = true, ParserType = typeof(TomlParsers.ByteSizeParser))]
	public uint BufferSize { get; set; } = 8192;

	[Toml(Optional = true)] public int ReusableBufferPoolSize { get; set; } = 24;

	[Toml(Optional = true, ParserType = typeof(TomlParsers.ByteSizeParser))]
	public int ReusableBufferInitCap { get; set; } = 0;

	[Toml(Optional = true, ParserType = typeof(TomlParsers.ByteSizeParser))]
	public int ReusableBufferMaxCap { get; set; } = 30 * 1024;

	[Toml(Optional = true)] public SslOptions? SslOptions { get; set; } = null;
}