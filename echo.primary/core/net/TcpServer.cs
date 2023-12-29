using System.Collections.Concurrent;
using echo.primary.utils;
using echo.primary.logging;

namespace echo.primary.core.net;

using System.Net;
using System.Net.Sockets;

public delegate ITcpProtocol TcpProtocolConstructor();

public class TcpServer(TcpSocketOptions socketOptions) : IDisposable {
	public TcpSocketOptions SocketOptions => socketOptions;
	public Logger Logger { get; set; } = new();

	public string Name { get; set; } = "TcpServer";

	internal readonly ThreadLocalPool<ReusableMemoryStream> ThreadLocalPool = new(
		() => new ReusableMemoryStream(
			socketOptions.ReusableBufferInitCap,
			socketOptions.ReusableBufferMaxCap
		),
		maxIdleSize: socketOptions.ReusableBufferPoolSize
	);

	private readonly List<Action> _beforeTcpServerListenHandlers = new();

	public event Action BeforeListen {
		add => _beforeTcpServerListenHandlers.Add(value);
		remove => _beforeTcpServerListenHandlers.Remove(value);
	}

	private readonly List<Action> _beforeTcpServerShutdownHandlers = new();

	public event Action BeforeShutdown {
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
		conn.Run(socketOptions, constructor());
	}

	private void OnAcceptSsl(Socket sock, TcpProtocolConstructor constructor) {
		if (_stopped) {
			sock.Close();
			return;
		}

		var conn = new TcpConnection(this, sock);
		Connections[conn] = 1;
		conn.RunSsl(socketOptions, socketOptions.SslOptions!, constructor());
	}

	public void Disconnect(TcpConnection connection) {
		if (_stopped) return;
		Connections.TryRemove(connection, out _);
	}

	private Socket? _sock;
	private bool _stopped;

	private delegate void OnAcceptFunc(Socket sock, TcpProtocolConstructor constructor);

	public async Task Start(string addr, ushort port, TcpProtocolConstructor constructor) {
		socketOptions.SslOptions?.Load();
		OnAcceptFunc doAccept = socketOptions.SslOptions == null ? OnAccept : OnAcceptSsl;

		var endpint = new IPEndPoint(IPAddress.Parse(addr), port);

		var sock = new Socket(endpint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
		sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, socketOptions.ReuseAddress);
		sock.SetSocketOption(
			SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse,
			socketOptions.ExclusiveAddressUse
		);
		if (sock.AddressFamily == AddressFamily.InterNetworkV6) {
			sock.DualMode = socketOptions.DualMode;
		}

		sock.Bind(endpint);

		foreach (var handler in _beforeTcpServerListenHandlers) {
			handler();
		}

		_sock = sock;
		sock.Listen(socketOptions.Backlog);
		Logger.Info(
			$"{Name} is listening @ {addr}:{port}, ssl: {socketOptions.SslOptions != null}, pid: {Environment.ProcessId}"
		);
		_stopped = false;

		while (!_stopped) {
			try {
				doAccept(await sock.AcceptAsync(), constructor);
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
			Logger.Close();
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