namespace echo.primary.core.io;

public interface IAsyncReader {
	Task<int> Read(byte[] buf, int timeoutMills);
	Task<int> Read(Memory<byte> buf, int timeoutMills);
	Task ReadExactly(byte[] buf, int timeoutMills);
	Task ReadExactly(Memory<byte> buf, int timeoutMills);
	Task<byte> ReadByte(int timeoutMills);
	Task<int> ReadAtLeast(Memory<byte> buf, int timeoutMills, int minimumBytes, bool throwWhenEnd = true);
	Task<int> ReadAtLeast(byte[] buf, int timeoutMills, int minimumBytes, bool throwWhenEnd = true);
}