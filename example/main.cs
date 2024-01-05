using echo.primary.logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Drawing;
using echo.primary.core.h2tp;
using echo.primary.utils;

using var host = new HostBuilder().Build();

/*
 * mkcert dev.local
 * openssl pkcs12 -in ./dev.local.pem -inkey ./dev.local-key.pem --export -out ./dev.local.pfx
 * rm ./*.pem
 */

var opts = TomlLoader.Load<ServerOptions>("./example/c.toml");
var server = new HttpServer(opts);

Log.AddAppender(
	new ColorfulConsoleAppender(
		"Console",
		schemas: new Dictionary<Level, ColorSchema> {
			{
				Level.Info, new ColorSchema(level: Color.Green, message: Color.Green)
			}
		}
	)
);

var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

var handler = new FsHandler(opts.FsOptions);

lifetime.ApplicationStarted.Register(() => { _ = server.Start(handler); });

lifetime.ApplicationStopping.Register(() => {
	server.Stop();
	Log.Close();
});

host.Start();
host.WaitForShutdown();