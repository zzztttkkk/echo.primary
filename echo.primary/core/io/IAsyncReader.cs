namespace echo.primary.core.io;

public interface IAsyncReader {
	Task<int> Read(byte[] buf);
	Task<int> Read(byte[] buf, int timeoutMills);
	Task<bool> ReadExactly(byte[] buf);
	Task<bool> ReadExactly(byte[] buf, int timeoutMills);
}