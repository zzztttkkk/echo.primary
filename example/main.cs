using echo.primary.core.net;
using echo.primary.logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using var host = new HostBuilder().Build();

/*
 * mkcert dev.local
 * openssl pkcs12 -in ./dev.local.pem -inkey ./dev.local-key.pem --export -out ./dev.local.pfx
 * rm ./*.pem
 */
var opts = new TcpSocketOptions(
	SslOptions: new SslOptions("../../../dev.local.pfx", Password: "123456")
);

var server = new TcpServer(opts);
server.Logger.AddAppender(new ConsoleAppender(""));

var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

lifetime.ApplicationStarted.Register(() => {
	server.Start(
		"0.0.0.0", 8080, () => new TcpEchoProtocol()
	).ContinueWith(
		t => {
			if (t.Exception != null) {
				server.Logger.Error($"{t.Exception}");
			}
		}
	);
});

lifetime.ApplicationStopping.Register(() => { server.Stop(); });

host.Start();
host.WaitForShutdown();