using System.Net.Security;
using System.Net.Sockets;
using echo.primary.logging;

namespace echo.primary.core.net;

public class TcpConnection(TcpServer server, Socket socket) : IDisposable {
	private SslStream? _sslStream;
	private byte[] _rbuf = null!;
	private readonly LinkedList<byte[]> _wbufs = new();
	private TaskCompletionSource? _wpromise;
	private bool _closed;
	private ITcpProtocol? _protocol;
	public Logger Logger => server.Logger;
	public Socket Socket { get; } = socket;

	private async Task DoRead() {
		while (!_closed) {
			int len;
			try {
				len = await Socket.ReceiveAsync(_rbuf);
			}
			catch (Exception e) {
				Close(e);
				return;
			}

			if (len < 1) break;
			_protocol!.DataReceived(_rbuf, len);
		}

		Close();
	}

	private async Task DoReadOverSsl() {
		while (!_closed) {
			int len;
			try {
				len = await _sslStream!.ReadAsync(_rbuf);
			}
			catch (Exception e) {
				Close(e);
				return;
			}

			if (len < 1) break;
			_protocol!.DataReceived(_rbuf, len);
		}

		Close();
	}

	private async Task DoWrite() {
		while (!_closed) {
			if (_wbufs.Count < 1) {
				_wpromise ??= new();
				await _wpromise.Task;
				_wpromise = null;
				if (_closed) break;
			}

			var first = _wbufs.First;
			var buf = first!.Value;
			_wbufs.RemoveFirst();

			try {
				await Socket.SendAsync(buf);
			}
			catch (Exception e) {
				Close(e);
				return;
			}
		}
	}

	private async Task DoWriteOverSsl() {
		while (!_closed) {
			if (_wbufs.Count < 1) {
				_wpromise ??= new();
				await _wpromise.Task;
				_wpromise = null;
				if (_closed) break;
			}

			var first = _wbufs.First;
			var buf = first!.Value;
			_wbufs.RemoveFirst();

			try {
				await _sslStream!.WriteAsync(buf);
			}
			catch (Exception e) {
				Close(e);
				return;
			}
		}
	}

	private void LaunchRWTasks() {
		_ = DoRead();
		_ = DoWrite();
	}

	private void LaunchSslRWTasks() {
		_ = DoReadOverSsl();
		_ = DoWriteOverSsl();
	}

	public void Write(byte[] buf) {
		_wbufs.AddLast(buf);
		_wpromise?.SetResult();
	}

	public void Flush() {
		_wpromise?.SetResult();
	}

	private async Task SslHandshake(SslOptions opts, ITcpProtocol protocol) {
		_sslStream = opts.RemoteCertificateValidationCallback != null
			? new SslStream(
				new NetworkStream(Socket, false), false, opts.RemoteCertificateValidationCallback
			)
			: new SslStream(
				new NetworkStream(Socket, false), false
			);

		TaskCompletionSource<IAsyncResult> ps = new();
		_sslStream.BeginAuthenticateAsServer(
			opts.Certificate,
			opts.ClientCertificateRequired,
			opts.Protocols,
			false,
			(v) => { ps.SetResult(v); },
			null
		);
		var result = await ps.Task;
		if (_closed) return;

		try {
			_sslStream.EndAuthenticateAsServer(result);
		}
		catch (Exception e) {
			server.Logger.Debug($"SslHandshakeFailed: {Socket.RemoteEndPoint} {e.Message}");
			Close(e);
			return;
		}

		_protocol = protocol;
		_protocol.ConnectionMade(this);
		LaunchSslRWTasks();
	}

	public void Run(TcpSocketOptions opts, ITcpProtocol protocol) {
		_protocol = protocol;
		ApplyOptions(opts);
		protocol.ConnectionMade(this);
		LaunchRWTasks();
	}

	public void RunSsl(TcpSocketOptions sockOpts, SslOptions sslOptions, ITcpProtocol protocol) {
		ApplyOptions(sockOpts);
		SslHandshake(sslOptions, protocol).ContinueWith(t => {
			if (t.Exception == null) return;
			server.Logger.Error($"{t.Exception}");
			Close(t.Exception);
		});
	}

	private void ApplyOptions(TcpSocketOptions opts) {
		if (opts.KeepAlive) {
			Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
		}

		if (opts.KeepAliveTime > 0) {
			Socket.SetSocketOption(
				SocketOptionLevel.Socket, SocketOptionName.TcpKeepAliveTime, opts.KeepAliveTime
			);
		}

		if (opts.KeepAliveInterval > 0) {
			Socket.SetSocketOption(
				SocketOptionLevel.Socket, SocketOptionName.TcpKeepAliveInterval, opts.KeepAliveInterval
			);
		}

		if (opts.KeepAliveRetryCount > 0) {
			Socket.SetSocketOption(
				SocketOptionLevel.Socket, SocketOptionName.TcpKeepAliveRetryCount, opts.KeepAliveRetryCount
			);
		}

		if (opts.NoDelay) {
			Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
		}

		Socket.ReceiveBufferSize = (int)opts.ReceiveBufferSize;
		Socket.SendBufferSize = (int)opts.SendBufferSize;
		_rbuf = new byte[opts.ReceiveBufferSize];
	}

	public void Close(Exception? exception = null) {
		if (_closed) return;
		_closed = true;

		_protocol?.ConnectionLost(exception);

		_wpromise?.SetResult();
		_wbufs.Clear();

		server.Disconnect(this);

		try {
			_sslStream?.Close();
			_sslStream?.Dispose();
			Socket.Close();
			Socket.Dispose();
		}
		catch (Exception e) {
			// ignored
		}
	}

	public void Dispose() {
		if (_closed) return;
		Close();
		GC.SuppressFinalize(this);
	}
}