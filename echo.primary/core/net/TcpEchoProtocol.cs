using System.Text;
using echo.primary.core.io;

namespace echo.primary.core.net;

public class TcpEchoProtocol : ITcpProtocol {
	private TcpConnection _connection = null!;

	public void ConnectionMade(TcpConnection conn) {
		_connection = conn;
		conn.Logger.Info($"ConnectionMade: {conn.Socket.RemoteEndPoint}");
		_ = Read();
	}

	private async Task Read() {
		var extReaderTmp = _connection.MemoryStreamMemoryStreamPool.Get();
		extReaderTmp.Capacity = 4096;
		_connection.OnClose += _ => { _connection.MemoryStreamMemoryStreamPool.Put(extReaderTmp); };

		var reader = new ExtAsyncReader(_connection, new BytesBuffer(extReaderTmp));
		var tmp = new MemoryStream();
		while (_connection.IsAlive) {
			await reader.ReadUntil(tmp, (byte)'\n', timeoutMills: 5_000, maxBytesSize: 4096);
			await DataReceived(tmp.GetBuffer().AsMemory()[..(int)tmp.Position]);
		}
	}

	private async Task DataReceived(ReadOnlyMemory<byte> bytes) {
		_connection.Logger.Info(
			$"DataReceived: {_connection.Socket.RemoteEndPoint} {bytes.Length} {Encoding.UTF8.GetString(bytes.Span).Trim()}"
		);
		await _connection.Write(bytes);
		await _connection.Flush();
	}

	public void ConnectionLost(Exception? exception) {
		_connection.Logger.Info($"ConnectionLost: {_connection.Socket.RemoteEndPoint} {exception?.Message}");
	}

	public void Dispose() {
		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if (_connection == null) return;
		if (!_connection.IsAlive) return;
		_connection.Dispose();
	}
}