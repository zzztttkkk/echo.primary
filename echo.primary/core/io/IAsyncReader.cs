namespace echo.primary.core.io;

public interface IAsyncReader {
	Task<int> Read(byte[] buf);
	Task<int> Read(Memory<byte> buf);
	Task<int> Read(byte[] buf, int timeoutMills);
	Task<int> Read(Memory<byte> buf, int timeoutMills);
	Task ReadExactly(Memory<byte> buf);
	Task ReadExactly(Memory<byte> buf, int timeoutMills);
	Task<byte> ReadByte();
	Task<byte> ReadByte(int timeoutMills);
	Task<int> ReadAtLeast(Memory<byte> buf, int minimumBytes, bool throwWhenEnd = true);
	Task<int> ReadAtLeast(Memory<byte> buf, int timeoutMills, int minimumBytes, bool throwWhenEnd = true);
}