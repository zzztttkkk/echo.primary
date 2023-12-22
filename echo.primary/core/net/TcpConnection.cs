using System.Net.Security;
using System.Net.Sockets;
using echo.primary.core.io;
using echo.primary.logging;

namespace echo.primary.core.net;

public partial class TcpConnection(TcpServer server, Socket socket) : IDisposable, IAsyncReader {
	private SslStream? _sslStream;
	private bool _closed;
	private ITcpProtocol? _protocol;
	private BufferedStream? _stream;

	public Logger Logger => server.Logger;
	public Socket Socket { get; } = socket;

	public bool IsAlive => !_closed && Socket.Connected;

	private void EnsureAlive() {
		if (_closed || _stream == null) {
			throw new Exception("conn is not alive");
		}
	}

	public async Task Write(byte[] v) {
		EnsureAlive();
		await _stream!.WriteAsync(v);
	}

	public Task Flush() {
		EnsureAlive();
		return _stream!.FlushAsync();
	}

	private async Task SslHandshake(SslOptions opts, ITcpProtocol protocol) {
		_sslStream = opts.RemoteCertificateValidationCallback != null
			? new SslStream(
				new NetworkStream(Socket, false), false, opts.RemoteCertificateValidationCallback
			)
			: new SslStream(
				new NetworkStream(Socket, false), false
			);

		TaskCompletionSource<IAsyncResult> tcs = new();
		_sslStream.BeginAuthenticateAsServer(
			opts.Certificate,
			opts.ClientCertificateRequired,
			opts.Protocols,
			false,
			(v) => { tcs.SetResult(v); },
			null
		);

		if (opts.HandshakeTimeoutMills > 0) {
			_ = Task.Delay(opts.HandshakeTimeoutMills).ContinueWith(t => {
				if (tcs.Task.IsCompleted) return;
				tcs.SetCanceled();
			});
		}

		var result = await tcs.Task;

		if (_closed) return;

		try {
			_sslStream.EndAuthenticateAsServer(result);
		}
		catch (Exception e) {
			server.Logger.Debug($"SslHandshakeFailed: {Socket.RemoteEndPoint} {e.Message}");
			Close(e);
			return;
		}

		_stream = new BufferedStream(_sslStream, Socket.SendBufferSize);
		_protocol = protocol;
		_protocol.ConnectionMade(this);
	}

	public void Run(TcpSocketOptions opts, ITcpProtocol protocol) {
		_protocol = protocol;
		ApplyOptions(opts);
		_stream = new BufferedStream(new NetworkStream(Socket), (int)opts.BufferSize);
		protocol.ConnectionMade(this);
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

		Socket.ReceiveBufferSize = (int)opts.BufferSize;
		Socket.SendBufferSize = (int)opts.BufferSize;
	}

	public void Close(Exception? exception = null) {
		if (_closed) return;
		_closed = true;

		_protocol?.ConnectionLost(exception);
		server.Disconnect(this);

		try {
			_protocol?.Dispose();

			_stream?.Close();
			_stream?.Dispose();

			_sslStream?.Close();
			_sslStream?.Dispose();

			try {
				Socket.Shutdown(SocketShutdown.Both);
			}
			catch (SocketException) {
				// ignored
			}

			Socket.Close();
			Socket.Dispose();
		}
		catch (Exception) {
			// ignored
		}
	}

	public void Dispose() {
		if (_closed) return;
		Close();
		GC.SuppressFinalize(this);
	}

	#region IAsyncReader

	public Task<int> Read(byte[] buf) {
		throw new NotImplementedException();
	}

	public Task<int> Read(Memory<byte> buf) {
		throw new NotImplementedException();
	}

	public Task<int> Read(byte[] buf, int timeoutMills) {
		throw new NotImplementedException();
	}

	public Task<int> Read(Memory<byte> buf, int timeoutMills) {
		throw new NotImplementedException();
	}

	public Task ReadExactly(Memory<byte> buf) {
		throw new NotImplementedException();
	}

	public Task ReadExactly(Memory<byte> buf, int timeoutMills) {
		throw new NotImplementedException();
	}

	public Task<byte> ReadByte() {
		throw new NotImplementedException();
	}

	public Task<byte> ReadByte(int timeoutMills) {
		throw new NotImplementedException();
	}

	public Task<int> ReadAtLeast(Memory<byte> buf, int minimumBytes, bool throwWhenEnd = true) {
		throw new NotImplementedException();
	}

	public Task<int> ReadAtLeast(Memory<byte> buf, int timeoutMills, int minimumBytes, bool throwWhenEnd = true) {
		throw new NotImplementedException();
	}

	#endregion IAsyncReader
}