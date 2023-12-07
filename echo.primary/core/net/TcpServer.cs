using System.Collections.Concurrent;
using echo.primary.logging;

namespace echo.primary.core.net;

using System.Net;
using System.Net.Sockets;

public delegate void BeforeTcpServerListenHandler();

public delegate void BeforeTcpServerShutdownHandler();

public delegate ITcpProtocol TcpProtocolConstructor();

delegate void OnAcceptFunc(Socket sock, TcpProtocolConstructor constructor);

public class TcpServer : IDisposable {
	public TcpSocketOptions TcpSocketOptions { get; } = new();
	public Logger Logger { get; } = new();

	public string Name { get; } = "TcpServer";

	public TcpServer() : this("TcpServer") {
	}

	public TcpServer(string name) {
		Name = name;
	}

	public TcpServer(TcpSocketOptions options) {
		TcpSocketOptions = options;
	}

	private readonly List<BeforeTcpServerListenHandler> _beforeTcpServerListenHandlers = new();

	public event BeforeTcpServerListenHandler BeforeListen {
		add => _beforeTcpServerListenHandlers.Add(value);
		remove => _beforeTcpServerListenHandlers.Remove(value);
	}

	private readonly List<BeforeTcpServerShutdownHandler> _beforeTcpServerShutdownHandlers = new();

	public event BeforeTcpServerShutdownHandler BeforeShutdown {
		add => _beforeTcpServerShutdownHandlers.Add(value);
		remove => _beforeTcpServerShutdownHandlers.Remove(value);
	}

	protected readonly ConcurrentDictionary<TcpConnection, byte> Connections = new();

	private void OnAccept(Socket sock, TcpProtocolConstructor constructor) {
		if (_stopped) {
			sock.Close();
			return;
		}

		var conn = new TcpConnection(this, sock);
		Connections[conn] = 1;
		conn.Run(TcpSocketOptions, constructor());
	}

	private void OnAcceptSsl(Socket sock, TcpProtocolConstructor constructor) {
		if (_stopped) {
			sock.Close();
			return;
		}

		var conn = new TcpConnection(this, sock);
		Connections[conn] = 1;
		conn.RunSsl(TcpSocketOptions, TcpSocketOptions.SslOptions!, constructor());
	}

	public void Disconnect(TcpConnection connection) {
		if (_stopped) return;
		Connections.TryRemove(connection, out _);
	}

	private Socket? _sock;
	private bool _stopped;

	public async Task Start(string addr, ushort port, TcpProtocolConstructor constructor) {
		TcpSocketOptions.SslOptions?.Load();
		OnAcceptFunc DoAccept = TcpSocketOptions.SslOptions == null ? OnAccept : OnAcceptSsl;

		var endpint = new IPEndPoint(IPAddress.Parse(addr), port);

		var sock = new Socket(endpint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
		sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, TcpSocketOptions.ReuseAddress);
		sock.SetSocketOption(
			SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse,
			TcpSocketOptions.ExclusiveAddressUse
		);
		if (sock.AddressFamily == AddressFamily.InterNetworkV6) {
			sock.DualMode = TcpSocketOptions.DualMode;
		}

		sock.Bind(endpint);

		foreach (var handler in _beforeTcpServerListenHandlers) {
			handler();
		}

		_sock = sock;
		sock.Listen(TcpSocketOptions.Backlog);
		Logger.Info($"{Name} is listening @ {addr}:{port}, pid: {Environment.ProcessId}");
		_stopped = false;

		while (!_stopped) {
			try {
				DoAccept(await sock.AcceptAsync(), constructor);
			}
			catch (SocketException e) {
				switch (e.SocketErrorCode) {
					case SocketError.Shutdown:
					case SocketError.OperationAborted:
					case SocketError.ConnectionReset:
					case SocketError.ConnectionRefused:
					case SocketError.ConnectionAborted: {
						break;
					}
					default: {
						OnError(e);
						break;
					}
				}
			}
		}
	}

	protected void OnError(SocketException error) {
		Logger.Debug($"{error.SocketErrorCode} ---------- {error}");
	}

	public void Stop() {
		if (_sock == null || _stopped) return;
		_stopped = true;

		Logger.Info($"{Name} is stopping");

		foreach (var conn in Connections.Keys) {
			conn.Close();
		}

		foreach (var val in _beforeTcpServerShutdownHandlers) {
			val();
		}

		Logger.Flush();

		try {
			_sock.Close();
			_sock.Dispose();
		}
		catch (ObjectDisposedException) {
		}
	}

	public void Dispose() {
		Stop();
		GC.SuppressFinalize(this);
	}
}