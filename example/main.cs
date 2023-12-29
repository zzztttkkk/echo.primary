using echo.primary.core.net;
using echo.primary.logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Drawing;
using echo.primary.core.h2tp;

using var host = new HostBuilder().Build();

/*
 * mkcert dev.local
 * openssl pkcs12 -in ./dev.local.pem -inkey ./dev.local-key.pem --export -out ./dev.local.pfx
 * rm ./*.pem
 */

var opts = new ServerOptions();

var server = new TcpServer(opts.TcpSocketOptions);
server.SocketOptions.ReusableBufferPoolSize = 48;

server.Logger.Name = "TcpServer";
server.Logger.AddAppender(
	new ColorfulConsoleAppender(
		"ColorfulConsoleAppend",
		schemas: new Dictionary<Level, ColorSchema>() {
			{
				Level.INFO, new ColorSchema(Level: Color.Green, Message: Color.Green)
			}
		}
	)
);

var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

lifetime.ApplicationStarted.Register(() => {
	server.Start(
		"0.0.0.0", 8080, () => new Version1Protocol(new HelloWorldHandler(), opts)
	).ContinueWith(
		t => {
			if (t.Exception == null) return;

			server.Logger.Error($"{t.Exception}");
		}
	);
});

lifetime.ApplicationStopping.Register(() => { server.Stop(); });

host.Start();
host.WaitForShutdown();