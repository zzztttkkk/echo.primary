namespace echo.primary.core.io;

public class ReadExt {
	private IAsyncReader src;
	private BytesBuffer buf = new();
	private byte[] _rbuf;

	ReadExt(IAsyncReader src, int size = 512) {
		this.src = src;
		_rbuf = new byte[size];
	}

	public async Task<int> ReadLine(BytesBuffer dist, int maxlen = 0, int timeoutmills = 0) {
		while (true) {
			int len;
			if (timeoutmills > 0) {
				len = await src.Read(_rbuf);
			}
			else {
				len = await src.Read(_rbuf, timeoutmills);
			}

			if (len < 1) return -1;

			var idx = Array.FindIndex(_rbuf, 0, len, v => v == 10);
		}
	}
}