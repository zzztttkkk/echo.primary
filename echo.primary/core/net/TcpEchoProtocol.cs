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
		var reader = new ExtAsyncReader(Connection);
		var tmp = new MemoryStream();
		while (Connection.IsAlive) {
			await reader.ReadUntil(tmp, (byte)'\n', timeoutMills: 5_000, maxBytesSize: 4096);
			await DataReceived(tmp.ToArray(), (int)tmp.Position);
		}
	}

	private async Task DataReceived(byte[] buf, int size) {
		var bytes = buf.Take(size).ToArray();
		Connection.Logger.Info(
			$"DataReceived: {Connection.Socket.RemoteEndPoint} {bytes.Length} {Encoding.UTF8.GetString(bytes).Trim()}"
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