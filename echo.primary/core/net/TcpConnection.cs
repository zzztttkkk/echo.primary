using System.Net.Security;
using System.Net.Sockets;
using echo.primary.core.io;
using echo.primary.logging;

namespace echo.primary.core.net;

public class TcpConnection(TcpServer server, Socket socket) : IDisposable, IAsyncReader, IAsyncWriter {
	private SslStream? _sslStream;
	private bool _closed;
	private ITcpProtocol? _protocol;
	private BufferedStream? _stream;

	public Logger Logger => server.Logger;
	public Socket Socket { get; } = socket;

	public bool IsAlive => !_closed && Socket.Connected;

	public bool IsOverSsl => _sslStream != null;

	private void EnsureAlive() {
		if (_closed || _stream == null) {
			throw new Exception("conn is not alive");
		}
	}

	public Task Write(byte[] v) {
		EnsureAlive();
		return _stream!.WriteAsync(v).AsTask();
	}

	public Task Write(MemoryStream ms) {
		EnsureAlive();
		return _stream!.WriteAsync(ms.ToArray()).AsTask();
	}
	public Task Write(ReadOnlyMemory<byte> ms) {
		EnsureAlive();
		return _stream!.WriteAsync(ms).AsTask();
	}

	public async Task SendFile(string filename) {
		EnsureAlive();
		await Socket.SendFileAsync(filename);
	}

	public Task Flush() {
		EnsureAlive();
		return _stream!.FlushAsync();
	}

	private async Task SslHandshake(SslOptions opts, ITcpProtocol protocol) {
		_sslStream = new SslStream(
			new NetworkStream(Socket, false),
			false,
			opts.RemoteCertificateValidationCallback
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
			_ = Task.Delay(opts.HandshakeTimeoutMills).ContinueWith(_ => {
				if (tcs.Task.IsCompleted) return;
				tcs.SetCanceled();
			});
		}

		var result = await tcs.Task;
		if (_closed) return;
		_sslStream.EndAuthenticateAsServer(result);

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


		try {
			_protocol?.ConnectionLost(exception);
		}
		catch {
			// ignored
		}

		try {
			server.Disconnect(this);
		}
		catch {
			// ignored
		}

		try {
			_protocol?.Dispose();
		}
		catch {
			// ignored
		}

		try {
			_stream?.Close();
		}
		catch {
			// ignored
		}


		try {
			_stream?.Dispose();
		}
		catch {
			// ignored
		}


		try {
			_sslStream?.Close();
		}
		catch {
			// ignored
		}


		try {
			_sslStream?.Dispose();
		}
		catch {
			// ignored
		}

		try {
			Socket.Shutdown(SocketShutdown.Both);
		}
		catch {
			// ignored
		}


		try {
			Socket.Close();
		}
		catch {
			// ignored
		}


		try {
			Socket.Dispose();
		}
		catch {
			// ignored
		}
	}

	public void Dispose() {
		if (_closed) return;
		Close();
		GC.SuppressFinalize(this);
	}

	#region IAsyncReader

	private static CancellationTokenSource AutoCancel(int timeoutMills) {
		var cts = new CancellationTokenSource();
		cts.CancelAfter(timeoutMills);
		return cts;
	}

	public async Task<int> Read(byte[] buf, int timeoutMills) {
		EnsureAlive();

		if (timeoutMills < 1) {
			return await _stream!.ReadAsync(buf);
		}

		var cts = AutoCancel(timeoutMills);
		try {
			return await _stream!.ReadAsync(buf, cts.Token);
		}
		finally {
			cts.Dispose();
		}
	}

	public async Task<int> Read(Memory<byte> buf, int timeoutMills) {
		EnsureAlive();

		if (timeoutMills < 1) {
			return await _stream!.ReadAsync(buf);
		}

		var cts = AutoCancel(timeoutMills);
		try {
			return await _stream!.ReadAsync(buf, cts.Token);
		}
		finally {
			cts.Dispose();
		}
	}

	public async Task ReadExactly(byte[] buf, int timeoutMills) {
		EnsureAlive();

		if (timeoutMills < 1) {
			await _stream!.ReadExactlyAsync(buf);
			return;
		}

		EnsureAlive();

		var cts = AutoCancel(timeoutMills);
		try {
			await _stream!.ReadExactlyAsync(buf, cts.Token);
		}
		finally {
			cts.Dispose();
		}
	}

	public async Task ReadExactly(Memory<byte> buf, int timeoutMills) {
		EnsureAlive();

		if (timeoutMills < 1) {
			await _stream!.ReadExactlyAsync(buf);
			return;
		}

		EnsureAlive();

		var cts = AutoCancel(timeoutMills);
		try {
			await _stream!.ReadExactlyAsync(buf, cts.Token);
		}
		finally {
			cts.Dispose();
		}
	}

	public async Task<int> ReadAtLeast(byte[] buf, int timeoutMills, int minimumBytes, bool throwWhenEnd) {
		EnsureAlive();

		if (timeoutMills < 1) {
			return await _stream!.ReadAtLeastAsync(
				buf,
				minimumBytes: minimumBytes,
				throwOnEndOfStream: throwWhenEnd
			);
		}

		EnsureAlive();

		var cts = AutoCancel(timeoutMills);
		try {
			return await _stream!.ReadAtLeastAsync(
				buf,
				minimumBytes: minimumBytes,
				throwOnEndOfStream: throwWhenEnd,
				cancellationToken: cts.Token
			);
		}
		finally {
			cts.Dispose();
		}
	}


	public async Task<int> ReadAtLeast(
		Memory<byte> buf,
		int timeoutMills,
		int minimumBytes,
		bool throwWhenEnd
	) {
		EnsureAlive();

		if (timeoutMills < 1) {
			return await _stream!.ReadAtLeastAsync(buf, minimumBytes: minimumBytes, throwOnEndOfStream: throwWhenEnd);
		}

		EnsureAlive();

		var cts = AutoCancel(timeoutMills);
		try {
			return await _stream!.ReadAtLeastAsync(
				buf,
				minimumBytes: minimumBytes,
				throwOnEndOfStream: throwWhenEnd,
				cancellationToken: cts.Token
			);
		}
		finally {
			cts.Dispose();
		}
	}

	public async Task<byte> ReadByte(int timeoutMills) {
		var tmp = new byte[1];
		await ReadExactly(tmp, timeoutMills);
		return tmp[0];
	}

	#endregion IAsyncReader
}