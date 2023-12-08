namespace echo.primary.core.net;

public interface ITcpProtocol : IDisposable {
	void ConnectionMade(TcpConnection conn);
	void ConnectionLost(Exception? exception);
}