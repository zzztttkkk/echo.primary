namespace echo.primary.core.tcp;

using System.Net;
using System.Net.Sockets;

public abstract record SocketOptions(
	bool ReuseAddress = false,
	bool ExclusiveAddressUse = false,
	bool DualMode = false,
	int Backlog = 128
);

public class Server {
	public Server(NLog.Logger logger) {
		Logger = logger;
	}

	public NLog.Logger Logger { get; }

	protected void BeforeListen() {
	}

	protected void OnAccept(SocketAsyncEventArgs args) {
	}

	public void Start(string addr, ushort port, SocketOptions? opts = null) {
		var endpint = new IPEndPoint(IPAddress.Parse(addr), port);

		var sock = new Socket(endpint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
		sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, opts?.ReuseAddress ?? false);
		sock.SetSocketOption(
			SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse,
			opts?.ExclusiveAddressUse ?? false
		);
		if (sock.AddressFamily == AddressFamily.InterNetworkV6) {
			sock.DualMode = opts?.DualMode ?? false;
		}

		sock.Bind(endpint);

		BeforeListen();

		sock.Listen(opts?.Backlog ?? 128);
		Logger.Info($"HttpServer is listening @ {addr}:{port}");

		var args = new SocketAsyncEventArgs();
		args.Completed += (sender, eventArgs) => { OnAccept(eventArgs); };
		args.AcceptSocket = null;
		if (!sock.AcceptAsync(args)) {
			OnAccept(args);
		}
	}
}