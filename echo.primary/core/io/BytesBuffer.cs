using System.Text;

namespace echo.primary.core.io;

public class BytesBuffer(MemoryStream ms, Encoding? encoding = null) {
	public BinaryReader Reader { get; } = new(ms, encoding: encoding ?? Encoding.UTF8, leaveOpen: true);
	public BinaryWriter Writer { get; } = new(ms, encoding: encoding ?? Encoding.UTF8, leaveOpen: true);
	public MemoryStream Stream { get; } = ms;
}