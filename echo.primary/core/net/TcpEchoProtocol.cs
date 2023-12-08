using System.Text;

namespace echo.primary.core.net;

public class TcpEchoProtocol : ITcpProtocol {
	private TcpConnection Connection = null!;

	public void ConnectionMade(TcpConnection conn) {
		Connection = conn;
		conn.Logger.Info($"ConnectionMade: {conn.Socket.RemoteEndPoint}");
		_ = Read();
	}

	private async Task Read() {
		while (Connection.Alive) {
			var buf = new byte[1024];
			var len = await Connection.Read(buf);
			if (len < 1) {
				break;
			}

			await DataReceived(buf, len);
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
	}
}