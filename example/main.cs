﻿using echo.primary.logging;
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

var opts = TomlLoader.Load<ServerOptions>("./c.toml");
var server = new HttpServer(opts);

server.Logger.Name = "HttpServer";
server.Logger.AddAppender(
	new ColorfulConsoleAppender(
		"Console",
		schemas: new Dictionary<Level, ColorSchema>() {
			{
				Level.INFO, new ColorSchema(Level: Color.Green, Message: Color.Green)
			}
		}
	)
);

var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

lifetime.ApplicationStarted.Register(() => {
	_ = server.Start(new HelloWorldHandler()).ContinueWith(t => {
		if (t.Exception == null) return;
	});
});

lifetime.ApplicationStopping.Register(() => { server.Stop(); });

host.Start();
host.WaitForShutdown();