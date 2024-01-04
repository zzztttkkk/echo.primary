using echo.primary.core.net;

namespace echo.primary.core.h2tp;

public class WebSocketProtocol {
	private TcpConnection _connection = null!;
	private MemoryStream _tmp = null!;

	public void Start(TcpConnection connection, MemoryStream tmp) {
		_connection = connection;
		_tmp = tmp;
		_connection.OnClose += ConnectionLost;
	}

	public void Dispose() {
		throw new NotImplementedException();
	}

	public void ConnectionLost(Exception? exception) {
		throw new NotImplementedException();
	}
}