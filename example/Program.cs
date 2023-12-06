using echo.primary.core.tcp;
using echo.primary.logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using var host = new HostBuilder().Build();

var server = new Server();
server.Logger.AddAppender(new ConsoleAppender("root", Level.TRACE));

var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

lifetime.ApplicationStarted.Register(() => { server.Start("127.0.0.1", 8080); });

lifetime.ApplicationStopping.Register(() => {
	server.Stop();
});

host.Start();
host.WaitForShutdown();