using echo.primary.core.net;

namespace echo.primary.core.h2tp;

public class WebSocketProtocol {
	private TcpConnection _connection = null!;
	private MemoryStream _tmp = null!;

	public void Start(TcpConnection connection, MemoryStream tmp) {
		_connection = connection;
		_tmp = tmp;
	}

	public void Dispose() {
		throw new NotImplementedException();
	}

	private async Task Read() {
		while (true) { }
	}

	public void ConnectionLost(Exception? exception) {
		throw new NotImplementedException();
	}
}