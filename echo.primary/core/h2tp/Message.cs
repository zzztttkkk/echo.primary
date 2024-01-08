using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace echo.primary.core.h2tp;

internal enum MessageReadStatus {
	None = 0,
	Fl1Ok,
	Fl2Ok,
	Fl3Ok,
	HeaderOk,
	BodyOk,
}

public class Message {
	internal readonly string[] Flps;
	internal Headers? Herders;
	internal MemoryStream Body = null!;

	protected Message() {
		Flps = new string[3];
	}

	public ReadOnlyMemory<byte> BodyBuffer => Body.GetBuffer().AsMemory()[..(int)Body.Position];
	public ReadOnlySpan<byte> BodyBufferSpan => Body.GetBuffer().AsSpan()[..(int)Body.Position];

	internal void Reset() {
		Herders?.Clear();
		Body.Position = 0;
		Body.SetLength(0);
	}
}

public class FileRef(
	string filename,
	Tuple<long, long>? range = null,
	bool viaSendFile = false,
	FileInfo? fileInfo = null
) {
	public readonly string Filename = filename;
	public readonly FileInfo FileInfo = fileInfo ?? new(filename);
	public bool ViaSendFile = viaSendFile;
}

public enum BodyType {
	None,
	PlainText,
	Binary,
	Json,
	File,
	Stream,
}