using echo.primary.logging;

namespace echo.primary.core.tcp;

using System.Net;
using System.Net.Sockets;

public record SocketOptions(
	bool ReuseAddress = false,
	bool ExclusiveAddressUse = false,
	bool DualMode = false,
	int Backlog = 128
);

public delegate void BeforeTcpServerListenHandler();

public delegate void BeforeTcpServerShutdownHandler();

public class Server : IDisposable {
	public SocketOptions SocketOptions { get; }
	public Logger Logger { get; }

	public string Name { get; }

	public Server() : this("TcpServer") {
	}

	public Server(string name) {
		Name = name;
		Logger = new Logger();
		SocketOptions = new SocketOptions();
	}

	public Server(Logger logger) {
		Name = "TcpServer";
		Logger = logger;
		SocketOptions = new SocketOptions();
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

	protected void OnAccept(Socket sock) {
		Logger.Info($"{sock.RemoteEndPoint}");
		sock.Close();
	}


	private Socket? _sock = null;
	private SocketAsyncEventArgs? _sockAsyncEventArgs = null;

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
		Logger.Info($"HttpServer is listening @ {addr}:{port}, pid: {Environment.ProcessId}");

		_sockAsyncEventArgs = new SocketAsyncEventArgs();
		_sockAsyncEventArgs.Completed += (sender, evt) => ProcessAccecpt();

		StartAccept();
	}

	private void StartAccept() {
		_sockAsyncEventArgs!.AcceptSocket = null;
		if (!_sock!.AcceptAsync(_sockAsyncEventArgs!)) {
			ProcessAccecpt();
		}
	}

	private void ProcessAccecpt() {
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
		if (_sock == null) return;

		Logger.Info("");

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