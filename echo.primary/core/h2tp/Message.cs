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

internal class FileRef(
	string filename,
	Tuple<long, long>? range = null,
	bool viaSendFile = false,
	FileInfo? fileInfo = null
) {
	public readonly string Filename = filename;
	public readonly FileInfo FileInfo = fileInfo ?? new(filename);
	public readonly Tuple<long, long>? Range = range;
	public readonly bool ViaSendFile = viaSendFile;
}

public enum BodyType {
	None,
	PlainText,
	Binary,
	Json,
	File,
	Stream,
}

public class Response : Message {
	internal FileRef? FileRef;
	internal Stream? CompressStream;
	internal CompressType? CompressType;
	internal BodyType? BodyType;
	internal Stream? Stream;
	public bool NoCompression { get; set; }

	public int StatusCode {
		get => string.IsNullOrEmpty(Flps[1]) ? 0 : int.Parse(Flps[1]);
		set {
			Flps[1] = value.ToString();
			Flps[2] = StatusToString.ToString((RfcStatusCode)value);
		}
	}

	public string StatusText {
		get => string.IsNullOrEmpty(Flps[2]) ? StatusToString.ToString((RfcStatusCode)StatusCode) : Flps[2];
		set => Flps[2] = value;
	}

	public Headers Headers {
		get {
			Herders ??= new Headers();
			return Herders;
		}
	}

	internal void EnsureWriteStream() {
		if (CompressType == null || NoCompression || CompressStream != null) return;

		switch (CompressType) {
			case h2tp.CompressType.Brotil:
				CompressStream = new BrotliStream(Body, CompressionLevel.Optimal, leaveOpen: true);
				Headers.Set(RfcHeader.ContentEncoding, "br");
				break;
			case h2tp.CompressType.Deflate:
				CompressStream = new DeflateStream(Body, CompressionLevel.Optimal, leaveOpen: true);
				Headers.Set(RfcHeader.ContentEncoding, "deflate");
				break;
			case h2tp.CompressType.GZip:
				CompressStream = new GZipStream(Body, CompressionLevel.Optimal, leaveOpen: true);
				Headers.Set(RfcHeader.ContentEncoding, "gzip");
				break;
			default:
				throw new UnreachableException();
		}
	}

	private void ResetBodyInternal() {
		BodyType = null;
		NoCompression = false;
		CompressStream?.Dispose();
		CompressStream = null;
		Stream?.Close();
		Stream?.Dispose();
		Stream = null;
		FileRef = null;
	}

	public void ResetBody() {
		ResetBodyInternal();

		Body.Position = 0;
		Body.SetLength(0);
		Headers.Del(RfcHeader.ContentType);
		EnsureWriteStream();
	}


	private void WriteBytes(ReadOnlySpan<byte> buf) {
		EnsureWriteStream();

		if (CompressStream != null) {
			CompressStream.Write(buf);
		}
		else {
			Body.Write(buf);
		}
	}

	private static readonly Exception WrittenException = new(
		$"the body has been written, see `this.{nameof(ResetBody)}"
	);

	public void Write(ReadOnlySpan<byte> buf) {
		if (BodyType != null && BodyType != h2tp.BodyType.Binary) {
			throw WrittenException;
		}

		BodyType ??= h2tp.BodyType.Binary;
		WriteBytes(buf);
	}

	public void Write(byte[] buf) => Write(buf.AsSpan());

	public void Write(ReadOnlyMemory<byte> buf) => Write(buf.Span);


	public void Write(string txt, Encoding? encoding = null) {
		if (BodyType != null && BodyType != h2tp.BodyType.PlainText) {
			throw WrittenException;
		}

		BodyType ??= h2tp.BodyType.PlainText;
		WriteBytes((encoding ?? Encoding.UTF8).GetBytes(txt).AsSpan());
	}

	public void Write(StringBuilder sb) => Write(sb.ToString());

	public void WriteJson(object val, JsonSerializerOptions? options = null) {
		if (BodyType != null) throw WrittenException;

		BodyType ??= h2tp.BodyType.Json;
		EnsureWriteStream();
		JsonSerializer.Serialize(CompressStream ?? Body, val, options);
	}

	public void WriteFile(
		string path,
		Tuple<long, long>? range = null,
		bool viaSendFile = false,
		FileInfo? fileinfo = null
	) {
		if (BodyType != null) throw WrittenException;

		BodyType ??= h2tp.BodyType.File;
		FileRef = new FileRef(path, range, viaSendFile, fileinfo);
	}

	public void WriteStream(Stream stream) {
		if (BodyType != null) throw WrittenException;

		BodyType ??= h2tp.BodyType.Stream;
		Stream = stream;
	}

	internal new void Reset() {
		base.Reset();
		ResetBodyInternal();
	}
}