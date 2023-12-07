using echo.primary.core.net;
using echo.primary.logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using var host = new HostBuilder().Build();

var server = new TcpServer();
server.Logger.AddAppender(new ConsoleAppender(""));

var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

lifetime.ApplicationStarted.Register(() => { _ = server.Start("0.0.0.0", 8080, () => new TcpEchoProtocol()); });

lifetime.ApplicationStopping.Register(() => { server.Stop(); });

host.Start();
host.WaitForShutdown();