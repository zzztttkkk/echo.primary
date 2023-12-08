namespace echo.primary.core.io;

public interface IAsyncReader {
	Task<int> Read(byte[] bf);
	Task<int> Read(byte[] bf, int timeoutmills);
	Task<bool> ReadExactly(byte[] buf);
	Task<bool> ReadExactly(byte[] buf, int timeoutmills);
}