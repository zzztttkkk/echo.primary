using System.Text;

namespace echo.primary.core.io;

public class BytesBuffer {
	private readonly MemoryStream ms;
	public BinaryReader Reader { get; }
	public BinaryWriter Writer { get; }

	public MemoryStream Stream { get; }

	public BytesBuffer(int cap) {
		ms = new(cap);
		Writer = new(ms, encoding: Encoding.UTF8, leaveOpen: true);
		Reader = new(ms, encoding: Encoding.UTF8, leaveOpen: true);
		Stream = ms;
	}
}