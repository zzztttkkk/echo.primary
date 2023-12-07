namespace echo.primary.core.net;

public class TcpEchoProtocol : ITcpProtocol {
	private TcpConnection Connection = null!;

	public void ConnectionMade(TcpConnection conn) {
		Connection = conn;
		conn.Logger.Info($"ConnectionMade: {conn.Socket.RemoteEndPoint}");
	}

	public void DataReceived(byte[] buf, int size) {
		var bytes = buf.Take(size).ToArray();
		Connection.Logger.Info($"DataReceived: {Connection.Socket.RemoteEndPoint} {bytes.Length} {bytes} ");
		Connection.Write(bytes);
	}

	public void ConnectionLost(Exception? exception) {
		Connection.Logger.Info($"ConnectionLost: {Connection.Socket.RemoteEndPoint}");
	}

	public void Dispose() {
	}
}