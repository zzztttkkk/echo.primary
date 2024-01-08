using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace echo.primary.core.h2tp;

internal class ResponseBodyRef {
	internal BodyType BodyType = BodyType.None;
	internal object? Value;

	internal void Clear() {
		if (BodyType == BodyType.Stream && Value != null) {
			((Stream)Value).Dispose();
		}

		BodyType = BodyType.None;
		Value = null;
	}
}

public class Response : Message {
	internal readonly ResponseBodyRef BodyRef = new();
	internal CompressType? CompressType;
	internal Stream? CompressStream;
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
		NoCompression = false;
		CompressStream?.Dispose();
		CompressStream = null;
		BodyRef.Clear();
	}

	public void ResetBody() {
		ResetBodyInternal();

		Body.Position = 0;
		Body.SetLength(0);
		Headers.Del(RfcHeader.ContentType);
		EnsureWriteStream();
	}


	internal void WriteBytes(ReadOnlySpan<byte> buf) {
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
		if (BodyRef.BodyType != BodyType.Binary) {
			throw WrittenException;
		}

		BodyRef.BodyType = BodyType.Binary;
		WriteBytes(buf);
	}

	public void Write(byte[] buf) => Write(buf.AsSpan());

	public void Write(ReadOnlyMemory<byte> buf) => Write(buf.Span);


	public void Write(string txt, Encoding? encoding = null) {
		if (BodyRef.BodyType != BodyType.PlainText) {
			throw WrittenException;
		}

		BodyRef.BodyType = BodyType.PlainText;
		WriteBytes((encoding ?? Encoding.UTF8).GetBytes(txt).AsSpan());
	}

	public void Write(StringBuilder sb) => Write(sb.ToString());

	public void WriteJson(object val, JsonSerializerOptions? options = null) {
		if (BodyRef.BodyType != BodyType.None) throw WrittenException;

		BodyRef.BodyType = BodyType.Json;
		EnsureWriteStream();
		JsonSerializer.Serialize(CompressStream ?? Body, val, options);
	}

	public void WriteFile(FileRef @ref) {
		if (BodyRef.BodyType != BodyType.None) throw WrittenException;
		BodyRef.BodyType = BodyType.File;
		BodyRef.Value = @ref;
	}

	public void WriteStream(Stream stream) {
		if (BodyRef.BodyType != BodyType.None) throw WrittenException;
		BodyRef.BodyType = BodyType.Stream;
		BodyRef.Value = stream;
	}

	internal new void Reset() {
		base.Reset();
		ResetBodyInternal();
	}
}