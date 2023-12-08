using echo.primary.core.net;

namespace echo.primary.core.http;

public class V11Protocol : ITcpProtocol {
	public void Dispose() {
	}

	public void ConnectionMade(TcpConnection conn) {
		_ = ReadRequests(conn);
	}

	private async Task ReadRequests(TcpConnection conn) {
		while (conn.IsAlive) {
		}
	}

	public void ConnectionLost(Exception? exception) {
	}
}