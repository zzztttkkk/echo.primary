namespace echo.primary.core.net;

public interface ITcpProtocol : IDisposable {
	void ConnectionMade(TcpConnection conn);
	void DataReceived(byte[] buf, int size);
	void ConnectionLost(Exception? exception);
}