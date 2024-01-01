using System.Text;
using echo.primary.core.io;

namespace echo.primary.core.net;

public class TcpEchoProtocol : ITcpProtocol {
	private TcpConnection Connection = null!;

	public void ConnectionMade(TcpConnection conn) {
		Connection = conn;
		conn.Logger.Info($"ConnectionMade: {conn.Socket.RemoteEndPoint}");
		_ = Read();
	}

	private async Task Read() {
		var extReaderTmp = Connection.MemoryStreamMemoryStreamPool.Get();
		extReaderTmp.Capacity = 4096;
		Connection.OnClose += () => {
			Connection.MemoryStreamMemoryStreamPool.Put(extReaderTmp);
		};

		var reader = new ExtAsyncReader(Connection, new BytesBuffer(extReaderTmp));
		var tmp = new MemoryStream();
		while (Connection.IsAlive) {
			await reader.ReadUntil(tmp, (byte)'\n', timeoutMills: 5_000, maxBytesSize: 4096);
			await DataReceived(tmp.GetBuffer().AsMemory()[..(int)tmp.Position]);
		}
	}

	private async Task DataReceived(ReadOnlyMemory<byte> bytes) {
		Connection.Logger.Info(
			$"DataReceived: {Connection.Socket.RemoteEndPoint} {bytes.Length} {Encoding.UTF8.GetString(bytes.Span).Trim()}"
		);
		await Connection.Write(bytes);
		await Connection.Flush();
	}

	public void ConnectionLost(Exception? exception) {
		Connection.Logger.Info($"ConnectionLost: {Connection.Socket.RemoteEndPoint} {exception?.Message}");
	}

	public void Dispose() {
		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if (Connection == null) return;
		if (!Connection.IsAlive) return;
		Connection.Dispose();
	}
}