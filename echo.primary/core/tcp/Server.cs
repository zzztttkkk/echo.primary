using System.Collections.Concurrent;
using echo.primary.logging;

namespace echo.primary.core.tcp;

using System.Net;
using System.Net.Sockets;

public delegate void BeforeTcpServerListenHandler();

public delegate void BeforeTcpServerShutdownHandler();

public class Server : IDisposable {
	public SocketOptions SocketOptions { get; } = new();
	public Logger Logger { get; } = new();

	public string Name { get; } = "TcpServer";

	public Server() : this("TcpServer") {
	}

	public Server(string name) {
		Name = name;
	}

	public Server(SocketOptions options) {
		SocketOptions = options;
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

	protected ConcurrentDictionary<Connection, byte> Connections = new();

	private void OnAccept(Socket sock) {
		Logger.Info($"{sock.RemoteEndPoint}");

		if (SocketOptions.KeepAlive) {
			sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
		}

		if (SocketOptions.KeepAliveTime > 0) {
			sock.SetSocketOption(
				SocketOptionLevel.Socket, SocketOptionName.TcpKeepAliveTime, SocketOptions.KeepAliveTime
			);
		}

		if (SocketOptions.KeepAliveInterval > 0) {
			sock.SetSocketOption(
				SocketOptionLevel.Socket, SocketOptionName.TcpKeepAliveInterval, SocketOptions.KeepAliveInterval
			);
		}

		if (SocketOptions.KeepAliveRetryCount > 0) {
			sock.SetSocketOption(
				SocketOptionLevel.Socket, SocketOptionName.TcpKeepAliveRetryCount, SocketOptions.KeepAliveRetryCount
			);
		}

		if (SocketOptions.NoDelay) {
			sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
		}
	}

	private Socket? _sock;
	private SocketAsyncEventArgs? _sockAsyncEventArgs;
	private bool _stopping;

	public void Start(string addr, ushort port) {
		var endpint = new IPEndPoint(IPAddress.Parse(addr), port);

		var sock = new Socket(endpint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
		sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, SocketOptions.ReuseAddress);
		sock.SetSocketOption(
			SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse,
			SocketOptions.ExclusiveAddressUse
		);
		if (sock.AddressFamily == AddressFamily.InterNetworkV6) {
			sock.DualMode = SocketOptions.DualMode;
		}

		sock.Bind(endpint);

		foreach (var handler in _beforeTcpServerListenHandlers) {
			handler();
		}

		_sock = sock;
		sock.Listen(SocketOptions.Backlog);
		Logger.Info($"{Name} is listening @ {addr}:{port}, pid: {Environment.ProcessId}");
		_stopping = false;

		_sockAsyncEventArgs = new SocketAsyncEventArgs();
		_sockAsyncEventArgs.Completed += (sender, evt) => ProcessAccept();

		StartAccept();
	}

	private void StartAccept() {
		if (_stopping) return;

		_sockAsyncEventArgs!.AcceptSocket = null;
		if (!_sock!.AcceptAsync(_sockAsyncEventArgs!)) {
			ProcessAccept();
		}
	}

	private void ProcessAccept() {
		var err = _sockAsyncEventArgs!.SocketError;
		switch (err) {
			case SocketError.Success: {
				OnAccept(_sockAsyncEventArgs!.AcceptSocket!);
				break;
			}
			case SocketError.Shutdown:
			case SocketError.OperationAborted:
			case SocketError.ConnectionReset:
			case SocketError.ConnectionRefused:
			case SocketError.ConnectionAborted: {
				break;
			}
			default: {
				OnError(err);
				break;
			}
		}

		StartAccept();
	}

	protected void OnError(SocketError error) {
		Logger.Debug($"{error}");
	}

	public void Stop() {
		if (_sock == null || _stopping) return;

		_stopping = true;

		Logger.Info($"{Name} is stopping");

		foreach (var val in _beforeTcpServerShutdownHandlers) {
			val();
		}

		Logger.Flush();

		try {
			_sock.Close();
			_sock.Dispose();
			_sockAsyncEventArgs?.Dispose();
		}
		catch (ObjectDisposedException) {
		}
	}

	public void Dispose() {
		Stop();
		GC.SuppressFinalize(this);
	}
}