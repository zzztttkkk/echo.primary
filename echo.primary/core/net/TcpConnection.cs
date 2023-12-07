using System.Net.Sockets;

namespace echo.primary.core.net;

public class TcpConnection : IDisposable {
	protected TcpServer _server;
	protected TcpSocketOptions _options;
	private byte[] _rbuf;
	private readonly LinkedList<byte[]> _wbufs = new();
	private TaskCompletionSource? _wpromise;
	private bool _closed = false;

	private void OnAsyncCompleted(object? sender, SocketAsyncEventArgs e) {
	}

	public TcpConnection(TcpServer server, Socket socket, TcpSocketOptions options) {
		_server = server;
		Socket = socket;
		_options = options;
	}

	public Socket Socket { get; }

	private async Task ReadTask(ITcpProtocol protocol) {
		while (!_closed) {
			var len = await Socket.ReceiveAsync(_rbuf);
			protocol.DataReceived(_rbuf, len);
		}
	}

	private async Task WriteTask() {
		while (!_closed) {
			if (_wbufs.Count < 1) {
				_wpromise ??= new();
				await _wpromise.Task;
			}

			var first = _wbufs.First;
			var buf = first!.Value;
			_wbufs.RemoveFirst();

			await Socket.SendAsync(buf);
		}
	}

	public void Write(byte[] buf) {
		_wbufs.AddLast(buf);
		_wpromise?.SetResult();
	}

	public void Run(ITcpProtocol protocol) {
		applyOptions();
		ReadTask(protocol).Start();
		WriteTask().Start();

		if (_options.SslOptions == null) {
		}
	}

	private void applyOptions() {
		if (_options.KeepAlive) {
			Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
		}

		if (_options.KeepAliveTime > 0) {
			Socket.SetSocketOption(
				SocketOptionLevel.Socket, SocketOptionName.TcpKeepAliveTime, _options.KeepAliveTime
			);
		}

		if (_options.KeepAliveInterval > 0) {
			Socket.SetSocketOption(
				SocketOptionLevel.Socket, SocketOptionName.TcpKeepAliveInterval, _options.KeepAliveInterval
			);
		}

		if (_options.KeepAliveRetryCount > 0) {
			Socket.SetSocketOption(
				SocketOptionLevel.Socket, SocketOptionName.TcpKeepAliveRetryCount, _options.KeepAliveRetryCount
			);
		}

		if (_options.NoDelay) {
			Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
		}

		Socket.ReceiveBufferSize = (int)_options.ReceiveBufferSize;
		Socket.SendBufferSize = (int)_options.SendBufferSize;
		_rbuf = new byte[_options.ReceiveBufferSize];
	}

	public void Close() {
		if (_closed) return;
		_closed = true;


		_server.Disconnect(this);

		try {
			try {
				Socket.Shutdown(SocketShutdown.Both);
			}
			catch (SocketException) {
			}

			Socket.Close();
			Socket.Dispose();
		}
		catch (ObjectDisposedException) {
		}
	}

	public void Dispose() {
		if (_closed) return;
		Close();
		GC.SuppressFinalize(this);
	}
}