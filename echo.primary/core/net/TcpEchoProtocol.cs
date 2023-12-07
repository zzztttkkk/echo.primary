namespace echo.primary.core.net;

public class TcpEchoProtocol : ITcpProtocol {
	private TcpConnection _connection;

	public void ConnectionMade(TcpConnection conn) {
		_connection = conn;
	}

	public void DataReceived(byte[] d, int size) {
	}

	public void ConnectionLost(Exception? exception, object? args) {
	}
}