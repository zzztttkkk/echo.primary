namespace echo.primary.core.io;

public interface IAsyncWriter {
	Task Write(byte[] buf);
	Task Write(MemoryStream ms);

	Task Write(ReadOnlyMemory<byte> ms);

	Task SendFile(string filename);
	Task Flush();
}