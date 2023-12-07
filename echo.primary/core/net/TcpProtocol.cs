namespace echo.primary.core.net;

public interface ITcpProtocol {
	void ConnectionMade(TcpConnection conn);
	void DataReceived(byte[] d, int size);
	void ConnectionLost(Exception? exception, object? args);
}