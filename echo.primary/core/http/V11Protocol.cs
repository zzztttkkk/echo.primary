using echo.primary.core.io;
using echo.primary.core.net;

namespace echo.primary.core.http;

public class V11Protocol : ITcpProtocol {
	public void Dispose() {
	}

	public void ConnectionMade(TcpConnection conn) {
		_ = ReadRequests(conn);
	}

	private async Task ReadRequests(TcpConnection conn) {
		var reader = new ExtAsyncReader(conn);
		var linecache = new MemoryStream(1024);
		while (conn.IsAlive) {
			var line = await reader.ReadLine(linecache);
			Console.WriteLine(line);
		}
	}

	public void ConnectionLost(Exception? exception) {
	}
}