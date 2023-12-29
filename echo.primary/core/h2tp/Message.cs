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

	internal void Reset() {
		Herders?.Clear();
		Body.Position = 0;
		Body.SetLength(0);
	}
}

public class Request : Message {
	internal Uri? _uri;

	public string Method {
		get => Flps[0];
		set => Flps[0] = value;
	}

	public Uri Uri => _uri!;

	public string Version {
		get => Flps[2];
		set => Flps[2] = value;
	}

	public Headers Headers {
		get {
			Herders ??= new Headers();
			return Herders;
		}
	}

	internal new void Reset() {
		base.Reset();
	}
}

internal class FileRef(string filename, Tuple<long, long>? range = null, bool viaSendFile = false) {
	public string filename = filename;
	public readonly FileInfo fileinfo = new(filename);
	public Tuple<long, long>? range = range;
	public bool viaSendFile = viaSendFile;
}

public enum BodyType {
	None,
	PlainText,
	Binary,
	JSON,
	File,
	Stream,
}

public interface ISerializer {
	string? ContentType { get; }
	void Serialize(Stream stream, object val);
}

public class Response : Message {
	internal FileRef? _fileRef;
	internal Stream? _compressStream;
	private CompressType? _compressType;
	private int _miniCompressionSize;
	private BodyType? _bodyType;
	internal Stream? _stream;

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

	internal void EnsureWriteStream(int miniCompressionSize, CompressType? compressType) {
		Body.Position = 0;
		Body.SetLength(0);

		_miniCompressionSize = miniCompressionSize;
		_compressType = compressType;

		if (_miniCompressionSize < 1) AutoCompressWriter();
	}

	private void ResetBodyTmp() {
		_bodyType = null;

		_compressStream?.Dispose();
		_compressStream = null;

		_stream?.Close();
		_stream?.Dispose();
		_stream = null;

		_fileRef = null;

		Body.Position = 0;
		Body.SetLength(0);
		Headers.Del(RfcHeader.ContentType);
		AutoCompressWriter();
	}

	public BodyType BodyType {
		get => _bodyType ?? BodyType.None;
		private set {
			ResetBodyTmp();
			_bodyType = value;
		}
	}

	private void AutoCompressWriter() {
		if (_compressType == null) return;

		switch (_compressType) {
			case CompressType.Brotil:
				_compressStream = new BrotliStream(Body!, CompressionLevel.Optimal, leaveOpen: true);
				Headers.Set(RfcHeader.ContentEncoding, "br");
				break;
			case CompressType.Deflate:
				_compressStream = new DeflateStream(Body!, CompressionLevel.Optimal, leaveOpen: true);
				Headers.Set(RfcHeader.ContentEncoding, "deflate");
				break;
			case CompressType.GZip:
				_compressStream = new GZipStream(Body!, CompressionLevel.Optimal, leaveOpen: true);
				Headers.Set(RfcHeader.ContentEncoding, "gzip");
				break;
			default:
				throw new UnreachableException();
		}
	}

	private void WriteBytes(ReadOnlySpan<byte> buf) {
		if (_compressStream != null) {
			_compressStream.Write(buf);
		}
		else {
			Body ??= new(buf.Length);
			Body.Write(buf);

			if (_compressType == null || Body.Position < _miniCompressionSize) return;

			var prevData = Body.ToArray();

			Body.Position = 0;
			AutoCompressWriter();
			_compressStream!.Write(prevData);
		}
	}

	public void Write(byte[] buf) {
		if (BodyType is not BodyType.Binary) {
			BodyType = BodyType.Binary;
		}

		WriteBytes(buf.AsSpan());
	}

	public void Write(string txt, Encoding? encoding = null) {
		if (BodyType is not BodyType.PlainText) {
			BodyType = BodyType.PlainText;
		}

		WriteBytes((encoding ?? Encoding.UTF8).GetBytes(txt).AsSpan());
	}

	public void Write(StringBuilder sb) => Write(sb.ToString());

	public void WriteJSON(object val, JsonSerializerOptions? options = null) {
		if (Body is { Position: > 0 }) {
			ResetBodyTmp();
		}

		BodyType = BodyType.JSON;

		JsonSerializer.Serialize(_compressStream ?? Body!, val, options);
	}

	public void WriteFile(string path, Tuple<long, long>? range = null, bool viaSendFile = false) {
		_bodyType = BodyType.File;
		_fileRef = new FileRef(path, range, viaSendFile);
	}

	public void WriteStream(Stream stream) {
		_bodyType = BodyType.Stream;
		_stream = stream;
	}

	internal new void Reset() {
		base.Reset();
		ResetBodyTmp();
	}
}